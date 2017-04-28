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

namespace EthereumApi
{
	public class Startup
	{
		public Startup(IHostingEnvironment env)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);


			builder.AddEnvironmentVariables();
			Configuration = builder.Build();
		}

		public IConfigurationRoot Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container
		public IServiceProvider ConfigureServices(IServiceCollection services)
		{
			var provider = services.BuildServiceProvider();
			var settings = provider.GetService<IBaseSettings>();

			services.AddSingleton(settings);

			services.RegisterAzureLogs(settings, "Api");
			services.RegisterAzureStorages(settings);
			services.RegisterAzureQueues(settings);

			services.RegisterServices();

			provider = services.BuildServiceProvider();

			var builder = services.AddMvc();

			builder.AddMvcOptions(o => { o.Filters.Add(new GlobalExceptionFilter(provider.GetService<ILog>())); });

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
	}
}
