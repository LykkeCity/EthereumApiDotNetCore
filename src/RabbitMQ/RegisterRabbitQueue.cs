﻿using Common;
using Common.Log;
using Core.Settings;
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
            //FIX BACK TO HOST
            string exchangeName = exchangePrefix + settings.RabbitMq.ExchangeEthereumCore;
            RabbitMqPublisherSettings rabbitMqSettings = new RabbitMqPublisherSettings
            {
                ConnectionString = $"amqp://{settings.RabbitMq.Username}:{settings.RabbitMq.Password}@{settings.RabbitMq.ExternalHost}:{settings.RabbitMq.Port}",
                ExchangeName = exchangeName
            };
            RabbitMqSubscriberSettings rabbitMqSubscriberSettings = new RabbitMqSubscriberSettings
            {
                ConnectionString = $"amqp://{settings.RabbitMq.Username}:{settings.RabbitMq.Password}@{settings.RabbitMq.ExternalHost}:{settings.RabbitMq.Port}",
                ExchangeName = exchangeName,
                IsDurable = true,
                QueueName = settings.RabbitMq.RoutingKey
            };

            RabbitMqPublisher<string> publisher = new RabbitMqPublisher<string>(rabbitMqSettings)
                .SetSerializer(new BytesSerializer())
                .SetPublishStrategy(new PublishStrategy(settings.RabbitMq.RoutingKey))
                .SetLogger(logger)
                .Start();

            //RabbitMqSubscriber<string> subscriber =
            //  new RabbitMqSubscriber<string>(rabbitMqSubscriberSettings)
            //    .SetMessageDeserializer(new BytesDeserializer())
            //    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy(settings.RabbitMq.RoutingKey))
            //    .SetLogger(logger)
            //    .Start();

            services.AddSingleton<IMessageProducer<string>>(publisher);
            services.AddSingleton<IRabbitQueuePublisher, RabbitQueuePublisher>();
        }
    }

    internal class PublishStrategy : IRabbitMqPublishStrategy
    {
        private readonly string _queue;

        public PublishStrategy(string queue)
        {
            _queue = queue;
        }

        public void Configure(RabbitMqPublisherSettings settings, IModel channel)
        {
            channel.ExchangeDeclare(exchange: settings.ExchangeName, type: ExchangeType.Fanout, durable: true);
        }

        public void Publish(RabbitMqPublisherSettings settings, IModel channel, byte[] body)
        {
            channel.BasicPublish(exchange: settings.ExchangeName,
                      routingKey: _queue,//remove
                      basicProperties: null,
                      body: body);
        }
    }
}

