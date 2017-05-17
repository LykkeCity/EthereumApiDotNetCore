using Core.Repositories;
using Core.Settings;
using Nethereum.Web3;
using RabbitMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Services
{
    public interface ICoinEventPublisher
    {
        Task PublishEvent(ICoinEvent coinEvent);
    }

    public class CoinEventPublisherService : ICoinEventPublisher
    {
        private readonly IBaseSettings _settings;
        private readonly IRabbitQueuePublisher _rabbitPublisher;

        public CoinEventPublisherService(IBaseSettings settings, IRabbitQueuePublisher rabbitPublisher)
        {
            _settings = settings;
            _rabbitPublisher = rabbitPublisher;
        }

        public async Task PublishEvent(ICoinEvent coinEvent)
        {
            string coinEventSerialized = Newtonsoft.Json.JsonConvert.SerializeObject(coinEvent);
            await _rabbitPublisher.PublshEvent(coinEventSerialized);
        }
    }
}
