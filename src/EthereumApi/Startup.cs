using System;
using AzureRepositories;
using Core.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services;
using Common.Log;
using RabbitMQ;

namespace EthereumApi
{
    public class Startup
    {
        public const string DefaultConnectionString = "UseDevelopmentStorage=true";
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }
        public static IServiceProvider ServiceProvider { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var settings = GetSettings(Configuration);
            services.AddSingleton<IBaseSettings>(settings.EthereumCore);

            services.AddSingleton(settings);

            services.RegisterAzureLogs(settings.EthereumCore, "Api");
            services.RegisterAzureStorages(settings.EthereumCore, settings.SlackNotifications);
            services.RegisterAzureQueues(settings.EthereumCore, settings.SlackNotifications);
            services.RegisterServices();

            ServiceProvider = services.BuildServiceProvider();
            services.RegisterRabbitQueue(settings.EthereumCore, ServiceProvider.GetService<ILog>());

            var builder = services.AddMvc();

            builder.AddMvcOptions(o => { o.Filters.Add(new GlobalExceptionFilter(ServiceProvider.GetService<ILog>())); });

            services.AddSwaggerGen(c =>
            {
                c.SingleApiVersion(new Swashbuckle.Swagger.Model.Info
                {
                    Version = "v1",
                    Title = "Ethereum.Api"
                });
            });

            ServiceProvider = services.BuildServiceProvider();
            ServiceProvider.ActivateRequestInterceptor();
            return ServiceProvider;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseCors((policyBuilder) =>
            {
                policyBuilder.AllowAnyHeader();
                policyBuilder.AllowAnyOrigin();
            });

            app.UseStatusCodePagesWithReExecute("/home/error");
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseSwagger();
            app.UseSwaggerUi();
        }

        static SettingsWrapper GetSettings(IConfigurationRoot configuration)
        {
            var connectionString = configuration.GetConnectionString("ConnectionString");
            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = DefaultConnectionString;

            var settings = GeneralSettingsReader.ReadGeneralSettings<SettingsWrapper>(connectionString);

            return settings;
        }
    }
}
