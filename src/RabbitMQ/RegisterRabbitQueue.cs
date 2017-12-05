using Common;
using Common.Log;
using Core.Settings;
using Lykke.Job.EthereumCore.Contracts.Events;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;

namespace RabbitMQ
{
    public static class RegisterRabbitQueueEx
    {
        public static void RegisterRabbitQueue(this IServiceCollection services, IBaseSettings settings, ILog logger, string exchangePrefix = "")
        {
            string exchangeName = exchangePrefix + settings.RabbitMq.ExchangeEthereumCore;
            string connectionString = $"amqp://{settings.RabbitMq.Username}:{settings.RabbitMq.Password}@{settings.RabbitMq.Host}:{settings.RabbitMq.Port}";

            #region Default

            RabbitMqPublisherSettings rabbitMqDefaultSettings = new RabbitMqPublisherSettings
            {
                ConnectionString = connectionString,
                ExchangeName = exchangeName
            };

            RabbitMqPublisher<string> publisher = new RabbitMqPublisher<string>(rabbitMqDefaultSettings)
                .SetSerializer(new BytesSerializer())
                .SetPublishStrategy(new PublishStrategy(settings.RabbitMq.RoutingKey))
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
                .SetPublishStrategy(new PublishStrategy(settings.RabbitMq.RoutingKey))
                .SetLogger(logger)
                .Start();

            #endregion

            services.AddSingleton<IMessageProducer<string>>(publisher);
            services.AddSingleton<IMessageProducer<HotWalletEvent>>(hotWalletCashoutEventPublisher);
            services.AddSingleton<IRabbitQueuePublisher, RabbitQueuePublisher>();
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

