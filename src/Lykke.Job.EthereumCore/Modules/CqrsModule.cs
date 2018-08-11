using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Job.EthereumCore.Contracts.Cqrs;
using Lykke.Job.EthereumCore.Contracts.Cqrs.Commands;
using Lykke.Job.EthereumCore.Contracts.Cqrs.Events;
using Lykke.Job.EthereumCore.Workflow.Handlers;
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.SettingsReader;

namespace Lykke.Job.EthereumCore.Modules
{
    public class CqrsModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;
        private readonly ILog _log;

        public CqrsModule(IReloadingManager<AppSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            Lykke.Messaging.Serialization.MessagePackSerializerFactory.Defaults.FormatterResolver = MessagePack.Resolvers.ContractlessStandardResolver.Instance;
            var rabbitMqSagasSettings = new RabbitMQ.Client.ConnectionFactory { Uri = _settings.CurrentValue.EthereumCore.Cqrs.RabbitConnectionString };

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>();

            var messagingEngine = new MessagingEngine(_log,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    { "SagasRabbitMq", new TransportInfo(rabbitMqSagasSettings.Endpoint.ToString(), rabbitMqSagasSettings.UserName, rabbitMqSagasSettings.Password, "None", "RabbitMq") }
                }),
                new RabbitMqTransportFactory());

            var sagasEndpointResolver = new RabbitMqConventionEndpointResolver(
                "SagasRabbitMq",
                "messagepack",
                environment: "lykke",
                exclusiveQueuePostfix: "k8s");

            builder.RegisterType<CashoutCommandHandler>().SingleInstance();
            builder.RegisterType<TransferCommandHandler>().SingleInstance();

            builder.Register(ctx =>
            {
                const string defaultRoute = "self";

                return new CqrsEngine(_log,
                    ctx.Resolve<IDependencyResolver>(),
                    messagingEngine,
                    new DefaultEndpointProvider(),
                    true,
                    Register.DefaultEndpointResolver(sagasEndpointResolver),
                    Register.BoundedContext(EthereumBoundedContext.Name)
                        .PublishingEvents(typeof(CashoutCompletedEvent), typeof(TransferCompletedEvent))
                            .With("events")
                        .ListeningCommands(typeof(StartCashoutCommand))
                            .On(defaultRoute)
                            .WithCommandsHandler<CashoutCommandHandler>()
                        .ListeningCommands(typeof(StartTransferCommand))
                            .On(defaultRoute)
                            .WithCommandsHandler<TransferCommandHandler>());

            })
            .As<ICqrsEngine>()
            .SingleInstance()
            .AutoActivate();
        }

        internal class AutofacDependencyResolver : IDependencyResolver
        {
            private readonly IComponentContext _context;

            public AutofacDependencyResolver([NotNull] IComponentContext kernel)
            {
                _context = kernel ?? throw new ArgumentNullException(nameof(kernel));
            }

            public object GetService(Type type)
            {
                return _context.Resolve(type);
            }

            public bool HasService(Type type)
            {
                return _context.IsRegistered(type);
            }
        }
    }
}
