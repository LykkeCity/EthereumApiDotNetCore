using Common;
using Common.Log;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Job.EthereumCore.Contracts.Events;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;
using Lykke.SettingsReader;

namespace Lykke.Service.RabbitMQ
{
    public static class RegisterRabbitQueueEx
    {
        public static void RegisterRabbitQueue(this IServiceCollection Services, IReloadingManager<RabbitMq> settings, ILog logger, string exchangePrefix = "")
        {
            var rabbitSettings = settings.CurrentValue;
            string exchangeName = exchangePrefix + rabbitSettings.ExchangeEthereumCore;
            string connectionString = $"amqp://{rabbitSettings.Username}:{rabbitSettings.Password}@{rabbitSettings.Host}:{rabbitSettings.Port}";

            #region Default

            RabbitMqPublisherSettings rabbitMqDefaultSettings = new RabbitMqPublisherSettings
            {
                ConnectionString = connectionString,
                ExchangeName = exchangeName
            };

            RabbitMqPublisher<string> publisher = new RabbitMqPublisher<string>(rabbitMqDefaultSettings)
                .SetSerializer(new BytesSerializer())
                .SetPublishStrategy(new PublishStrategy(rabbitSettings.RoutingKey))
                .SetLogger(logger)
                .Start();

            #endregion

            #region Hotwallet

            RabbitMqPublisherSettings rabbitMqHotwalletSettings = new RabbitMqPublisherSettings
            {
                ConnectionString = connectionString,
                ExchangeName = $"{exchangeName}.hotwallet"
            };

            RabbitMqPublisher<HotWalletEvent> hotWalletCashoutEventPublisher = new RabbitMqPublisher<HotWalletEvent>(rabbitMqHotwalletSettings)
                .SetSerializer(new BytesSerializer<HotWalletEvent>())
                .SetPublishStrategy(new PublishStrategy(rabbitSettings.RoutingKey))
                .SetLogger(logger)
                .Start();

            #endregion

            Services.AddSingleton<IMessageProducer<string>>(publisher);
            Services.AddSingleton<IMessageProducer<HotWalletEvent>>(hotWalletCashoutEventPublisher);
            Services.AddSingleton<IRabbitQueuePublisher, RabbitQueuePublisher>();
        }
    }

    internal class PublishStrategy : IRabbitMqPublishStrategy
    {
        private readonly string _queue;
        private readonly string _exchangeName;

        public PublishStrategy(string queue, string exchangeName = null)
        {
            _queue = queue;
            _exchangeName = exchangeName;
        }

        public void Configure(RabbitMqPublisherSettings settings, IModel channel)
        {
            channel.ExchangeDeclare(exchange: _exchangeName ?? settings.ExchangeName, type: ExchangeType.Fanout, durable: true);
        }

        public void Publish(RabbitMqPublisherSettings settings, IModel channel, byte[] body)
        {
            channel.BasicPublish(exchange: _exchangeName ?? settings.ExchangeName,
                      routingKey: _queue,//remove
                      basicProperties: null,
                      body: body);
        }
    }
}

