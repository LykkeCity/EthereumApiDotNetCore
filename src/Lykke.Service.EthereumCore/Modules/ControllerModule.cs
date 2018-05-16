using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.AttributeFilters;
using Common.Log;
using Lykke.Service.EthereumCore.AzureRepositories;
using Lykke.Service.EthereumCore.Core.Services;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Core.Utils;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.RabbitMQ;
using Lykke.SettingsReader;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq;
using Module = Autofac.Module;

namespace Lykke.Service.EthereumCore.Modules
{
    public class ControllerModule : Module
    {
        private readonly ILog _log;

        public ControllerModule(ILog log)
        {
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var controllerTypes = typeof(ControllerModule).Assembly.GetTypes().Where(x => typeof(Controller).IsAssignableFrom(x));
            controllerTypes.ForEach(@type =>
            {
                builder.RegisterType(@type).WithAttributeFiltering();
            });
        }
    }

    public static class Extensions
    {
        public static void RegisterControllers(this ContainerBuilder builder)
        {
            var controllerTypes = typeof(ControllerModule).Assembly.GetTypes().Where(x => typeof(Controller).IsAssignableFrom(x));
            controllerTypes.ForEach(@type =>
            {
                builder.RegisterType(@type).WithAttributeFiltering();
            });
        }
    }
}
