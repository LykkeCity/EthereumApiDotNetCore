using System;
using Lykke.Service.EthereumCore.AzureRepositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Lykke.Service.EthereumCore.Services;
using Common.Log;
using Lykke.Service.RabbitMQ;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.SettingsReader;
using Lykke.Service.EthereumCore.Modules;
using Lykke.Logs;
using AzureStorage.Tables;
using Lykke.SlackNotification.AzureQueue;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Services;
using Lykke.Common.ApiLibrary.Middleware;
using Autofac.Extensions.DependencyInjection;
using Autofac;
using Lykke.Service.EthereumCore;

namespace Lykke.Service.EthereumCore
{
    public class Startup
    {
        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; private set; }
        public IConfigurationRoot Configuration { get; }
        public ILog Log { get; private set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            Environment = env;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            try
            {
                var mvcBuilder = services.AddMvc()
                    .AddJsonOptions(options =>
                    {
                        options.SerializerSettings.ContractResolver =
                            new Newtonsoft.Json.Serialization.DefaultContractResolver();
                    });

                services.AddSwaggerGen(options =>
                {
                    options.DefaultLykkeConfiguration("v1", "EthereumCore API");
                });

                var builder = new ContainerBuilder();
                var appSettings = Configuration.LoadSettings<AppSettings>();
                Log = CreateLogWithSlack(services, appSettings);
                mvcBuilder.AddMvcOptions(o => { o.Filters.Add(new GlobalExceptionFilter(Log)); });
                builder.RegisterModule(new ServiceModule(appSettings, Log));
                builder.Populate(services);
                ApplicationContainer = builder.Build();

                return new AutofacServiceProvider(ApplicationContainer);
            }
            catch (Exception ex)
            {
                Log?.WriteFatalErrorAsync(nameof(Startup), nameof(ConfigureServices), "", ex).GetAwaiter().GetResult();
                throw;
            }
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            try
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                //app.UseLykkeMiddleware("EthereumCore", ex => new { Message = "Technical problem" });

                app.UseMvc();
                app.UseSwagger(c =>
                {
                    c.PreSerializeFilters.Add((swagger, httpReq) => swagger.Host = httpReq.Host.Value);
                });
                app.UseSwaggerUI(x =>
                {
                    x.RoutePrefix = "swagger/ui";
                    x.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                });
                app.UseStaticFiles();

                appLifetime.ApplicationStarted.Register(() => StartApplication().GetAwaiter().GetResult());
                appLifetime.ApplicationStopping.Register(() => StopApplication().GetAwaiter().GetResult());
                appLifetime.ApplicationStopped.Register(() => CleanUp().GetAwaiter().GetResult());
            }
            catch (Exception ex)
            {
                Log?.WriteFatalErrorAsync(nameof(Startup), nameof(Configure), "", ex).GetAwaiter().GetResult();
                throw;
            }
        }

        private async Task StartApplication()
        {
            try
            {
                // NOTE: Service not yet recieve and process requests here

                await ApplicationContainer.Resolve<IStartupManager>().StartAsync();

                await Log.WriteMonitorAsync("", "", "Started");
            }
            catch (Exception ex)
            {
                await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StartApplication), "", ex);
                throw;
            }
        }

        private async Task StopApplication()
        {
            try
            {
                // NOTE: Service still can recieve and process requests here, so take care about it if you add logic here.

                await ApplicationContainer.Resolve<IShutdownManager>().StopAsync();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StopApplication), "", ex);
                }
                throw;
            }
        }

        private async Task CleanUp()
        {
            try
            {
                // NOTE: Service can't recieve and process requests here, so you can destroy all resources

                if (Log != null)
                {
                    await Log.WriteMonitorAsync("", "", "Terminating");
                }

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(CleanUp), "", ex);
                    (Log as IDisposable)?.Dispose();
                }
                throw;
            }
        }

        private static ILog CreateLogWithSlack(IServiceCollection services, IReloadingManager<AppSettings> settings)
        {
            var consoleLogger = new LogToConsole();
            var aggregateLogger = new AggregateLogger();

            aggregateLogger.AddLog(consoleLogger);

            var dbLogConnectionStringManager = settings.Nested(x => x.EthereumCore.Db.LogsConnString);
            var dbLogConnectionString = dbLogConnectionStringManager.CurrentValue;

            if (string.IsNullOrEmpty(dbLogConnectionString))
            {
                consoleLogger.WriteWarningAsync(nameof(Startup), nameof(CreateLogWithSlack), "Table loggger is not inited").Wait();
                return aggregateLogger;
            }

            if (dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}"))
                throw new InvalidOperationException($"LogsConnString {dbLogConnectionString} is not filled in settings");

            var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(
                AzureTableStorage<LogEntity>.Create(dbLogConnectionStringManager, "EthereumCoreLog", consoleLogger),
                consoleLogger);

            // Creating slack notification service, which logs own azure queue processing messages to aggregate log
            var slackService = services.UseSlackNotificationsSenderViaAzureQueue(new Lykke.AzureQueueIntegration.AzureQueueSettings
            {
                ConnectionString = settings.CurrentValue.SlackNotifications.AzureQueue.ConnectionString,
                QueueName = settings.CurrentValue.SlackNotifications.AzureQueue.QueueName
            }, aggregateLogger);

            var slackNotificationsManager = new LykkeLogToAzureSlackNotificationsManager(slackService, consoleLogger);

            // Creating azure storage logger, which logs own messages to concole log
            var azureStorageLogger = new LykkeLogToAzureStorage(
                persistenceManager,
                slackNotificationsManager,
                consoleLogger);

            azureStorageLogger.Start();

            aggregateLogger.AddLog(azureStorageLogger);

            return aggregateLogger;
        }
    }

    //static AppSettings GetSettings(IConfigurationRoot configuration)
    //{
    //    var connectionString = configuration.GetConnectionString("ConnectionString");
    //    if (string.IsNullOrWhiteSpace(connectionString))
    //        connectionString = DefaultConnectionString;

    //    var settings = GeneralSettingsReader.ReadGeneralSettings<AppSettings>(connectionString);

    //    return settings;
    //}
}


//public class OldStartup
//{
//    public const string DefaultConnectionString = "UseDevelopmentStorage=true";
//    public OldStartup(IHostingEnvironment env)
//    {
//        var builder = new ConfigurationBuilder()
//            .SetBasePath(env.ContentRootPath)
//            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
//            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
//            .AddEnvironmentVariables();

//        builder.AddEnvironmentVariables();
//        Configuration = builder.Build();
//    }

//    public IConfigurationRoot Configuration { get; }
//    public static IServiceProvider ServiceProvider { get; private set; }

//    // This method gets called by the runtime. Use this method to add Lykke.Service.EthereumCore.Services to the container
//    public IServiceProvider ConfigureServices(IServiceCollection Services)
//    {
//        var settings = GetSettings(Configuration);
//        Services.AddSingleton<IBaseSettings>(settings.EthereumCore);

//        Services.AddSingleton(settings);

//        Services.RegisterAzureLogs(settings.EthereumCore, "Api");
//        Services.RegisterAzureStorages(settings.EthereumCore, settings.SlackNotifications);
//        Services.RegisterAzureQueues(settings.EthereumCore, settings.SlackNotifications);
//        Services.RegisterServices();

//        ServiceProvider = Services.BuildServiceProvider();
//        Services.RegisterRabbitQueue(settings.EthereumCore, ServiceProvider.GetService<ILog>());

//        var builder = Services.AddMvc();

//        builder.AddMvcOptions(o => { o.Filters.Add(new GlobalExceptionFilter(ServiceProvider.GetService<ILog>())); });

//        Services.AddSwaggerGen(c =>
//        {
//            c.SingleApiVersion(new Swashbuckle.Swagger.Model.Info
//            {
//                Version = "v1",
//                Title = "Ethereum.Api"
//            });
//        });

//        ServiceProvider = Services.BuildServiceProvider();
//        ServiceProvider.ActivateRequestInterceptor();
//        return ServiceProvider;
//    }

//    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
//    public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
//    {
//        app.UseCors((policyBuilder) =>
//        {
//            policyBuilder.AllowAnyHeader();
//            policyBuilder.AllowAnyOrigin();
//        });

//        app.UseStatusCodePagesWithReExecute("/home/error");
//        app.UseMvc(routes =>
//        {
//            routes.MapRoute(
//                name: "default",
//                template: "{controller=Home}/{action=Index}/{id?}");
//        });

//        app.UseSwagger();
//        app.UseSwaggerUi();
//    }
//}

