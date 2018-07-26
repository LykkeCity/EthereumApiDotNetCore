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
using Lykke.RabbitMq.Azure;
using AzureStorage.Blob;
using Lykke.Job.EthereumCore.Contracts.Events.LykkePay;

namespace Lykke.Service.RabbitMQ
{
    public static class RegisterRabbitQueueEx
    {
        public static void RegisterRabbitQueue(this IServiceCollection Services,
            IReloadingManager<Lykke.Service.EthereumCore.Core.Settings.RabbitMq> settings,
            IReloadingManager<string> dataConnString,
            ILog logger,
            string exchangePrefix = "")
        {
            var queueRepository = new MessagePackBlobPublishingQueueRepository(AzureBlobStorage.Create(dataConnString), "ethereumCoreRabbitMQ");
            var queueRepositoryHW = new MessagePackBlobPublishingQueueRepository(AzureBlobStorage.Create(dataConnString), "ethereumCoreRabbitMqHotwallet");
            var queueRepositoryLP = new MessagePackBlobPublishingQueueRepository(AzureBlobStorage.Create(dataConnString), "ethereumCoreRabbitMqLykkePay");
            var rabbitSettings = settings.CurrentValue;
            string exchangeName = exchangePrefix + rabbitSettings.ExchangeEthereumCore;
            string connectionString = $"amqp://{rabbitSettings.Username}:{rabbitSettings.Password}@{rabbitSettings.Host}:{rabbitSettings.Port}";

            #region Default

            RabbitMqSubscriptionSettings rabbitMqDefaultSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = connectionString,
                ExchangeName = exchangeName,
                DeadLetterExchangeName = $"{exchangeName}.dlx",
                RoutingKey = ""
            };

            RabbitMqPublisher<string> publisher = new RabbitMqPublisher<string>(rabbitMqDefaultSettings)
                .SetSerializer(new BytesSerializer())
                .SetPublishStrategy(new PublishStrategy(rabbitSettings.RoutingKey))
                .SetLogger(logger)
                .SetQueueRepository(queueRepository)
                .Start();

            #endregion

            #region Hotwallet

            RabbitMqSubscriptionSettings rabbitMqHotwalletSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = connectionString,
                ExchangeName = $"{exchangeName}.hotwallet",
                DeadLetterExchangeName = $"{exchangeName}.hotwallet.dlx",
                RoutingKey = ""

            };

            RabbitMqPublisher<HotWalletEvent> hotWalletCashoutEventPublisher = new RabbitMqPublisher<HotWalletEvent>(rabbitMqHotwalletSettings)
                .SetSerializer(new BytesSerializer<HotWalletEvent>())
                .SetPublishStrategy(new PublishStrategy(rabbitSettings.RoutingKey))
                .SetLogger(logger)
                .SetQueueRepository(queueRepositoryHW)
                .Start();

            #endregion

            #region LykkePay

            RabbitMqSubscriptionSettings rabbitMqLykkePaySettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = connectionString,
                ExchangeName = $"{exchangeName}.lykkepay",
                DeadLetterExchangeName = $"{exchangeName}.lykkepay.dlx",
                RoutingKey = ""

            };

            RabbitMqPublisher<TransferEvent> lykkePayEventPublisher = new RabbitMqPublisher<TransferEvent>(rabbitMqLykkePaySettings)
                .SetSerializer(new BytesSerializer<TransferEvent>())
                .SetPublishStrategy(new PublishStrategy(rabbitSettings.RoutingKey))
                .SetLogger(logger)
                .SetQueueRepository(queueRepositoryLP)
                .Start();

            #endregion

            Services.AddSingleton<IMessageProducer<string>>(publisher);
            Services.AddSingleton<IMessageProducer<HotWalletEvent>>(hotWalletCashoutEventPublisher);
            Services.AddSingleton<IMessageProducer<TransferEvent>>(lykkePayEventPublisher);
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

        public void Configure(RabbitMqSubscriptionSettings settings, IModel channel)
        {
            channel.ExchangeDeclare(exchange: _exchangeName ?? settings.ExchangeName, type: ExchangeType.Fanout, durable: true);
        }

        public void Publish(RabbitMqSubscriptionSettings settings, IModel channel, RawMessage message)
        {
            channel.BasicPublish(exchange: _exchangeName ?? settings.ExchangeName,
                routingKey: _queue,
                basicProperties: null,
                body: message.Body);
        }
    }
}

