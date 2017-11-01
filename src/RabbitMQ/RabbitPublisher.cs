using Common;
using Lykke.Job.EthereumCore.Contracts.Events;
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
        private readonly IMessageProducer<CoinAdapterCreationEvent> _coinCreationPublisher;
        private readonly Dictionary<Type, MessageProducerWrapper> _messageProducerDictionary =
            new Dictionary<Type, MessageProducerWrapper>();

        public RabbitQueuePublisher(IMessageProducer<string> publisher,
            IMessageProducer<CoinAdapterCreationEvent> coinCreationPublisher)
        {
            _publisher = publisher;
            _coinCreationPublisher = coinCreationPublisher;

            #region String

            MessageProducerWrapper strringWrapper = CreateWrapper(typeof(string), _publisher);
            _messageProducerDictionary.Add(typeof(string), strringWrapper);

            #endregion

            #region CoinAdapterCreationMessage

            MessageProducerWrapper CoinAdapterCreationMessageWrapper = CreateWrapper(typeof(CoinAdapterCreationEvent), _coinCreationPublisher);
            _messageProducerDictionary.Add(typeof(CoinAdapterCreationEvent), CoinAdapterCreationMessageWrapper);

            #endregion
        }

        public async Task PublshEvent(string rabbitEvent)
        {
            await _publisher.ProduceAsync(rabbitEvent);
        }
    }
}
