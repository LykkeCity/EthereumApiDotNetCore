using System.Linq;
using Autofac;
using Autofac.Features.AttributeFilters;
using Common.Log;
using Microsoft.AspNetCore.Mvc;
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
