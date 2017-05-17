using Common;
using Lykke.RabbitMqBroker.Publisher;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ
{
    public interface IRabbitQueuePublisher
    {
        Task PublshEvent(string rabbitEvent);
    }

    public class RabbitQueuePublisher : IRabbitQueuePublisher
    {
        private IMessageProducer<string> _publisher;

        public RabbitQueuePublisher(IMessageProducer<string> publisher)
        {
            _publisher = publisher;
        }

        public async Task PublshEvent(string rabbitEvent)
        {
            await _publisher.ProduceAsync(rabbitEvent);
        }
    }
}
