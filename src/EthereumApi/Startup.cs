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
using EthereumApi.Middleware;

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
        public IServiceProvider ServiceProvider { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var settings = GetSettings(Configuration);
            services.AddSingleton<IBaseSettings>(settings);

            services.AddSingleton(settings);

            services.RegisterAzureLogs(settings, "Api");
            services.RegisterAzureStorages(settings);
            services.RegisterAzureQueues(settings);
            services.RegisterServices();

            ServiceProvider = services.BuildServiceProvider();
            services.RegisterRabbitQueue(settings, ServiceProvider.GetService<ILog>());

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

            return services.BuildServiceProvider();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseCors((policyBuilder) =>
            {
                policyBuilder.AllowAnyHeader();
                policyBuilder.AllowAnyOrigin();
            });

            app.RegisterExceptionHandler(ServiceProvider.GetService<ILog>());

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

        static BaseSettings GetSettings(IConfigurationRoot configuration)
        {
            var connectionString = configuration.GetConnectionString("ConnectionString");
            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = DefaultConnectionString;

            var settings = GeneralSettingsReader.ReadGeneralSettings<SettingsWrapper>(connectionString);

            return settings.EthereumCore;
        }
    }
}
