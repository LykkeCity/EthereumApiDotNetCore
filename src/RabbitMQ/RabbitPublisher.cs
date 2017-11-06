using Common;
using Lykke.Job.EthereumCore.Contracts.Events;
using Lykke.RabbitMqBroker.Publisher;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private MessageProducerWrapper CreateWrapper(Type type, object messageProducer)
        {
            Type producerType = typeof(MessageProducerWrapper<>).MakeGenericType(type);
            var @interface = messageProducer?.GetType().GetInterfaces().FirstOrDefault();
            var constructors = producerType.GetConstructors();
            var constructor = constructors.FirstOrDefault();
            var wrapper = (MessageProducerWrapper)constructor.Invoke(new object[] { messageProducer });

            return wrapper;
        }

    }

    #region Support Types

    abstract public class MessageProducerWrapper
    {
        public abstract Task SendMessageAsync(object message);
    }

    public class MessageProducerWrapper<T> : MessageProducerWrapper where T : class
    {
        public IMessageProducer<T> _producer { get; private set; }

        public MessageProducerWrapper(IMessageProducer<T> producer)
        {
            _producer = producer;
        }

        public async Task SendMessageAsync(T message)
        {
            await _producer.ProduceAsync(message);
        }

        public override async Task SendMessageAsync(object message)
        {
            var castedObject = message as T;
            if (message == null)
            {
                throw new InvalidCastException($"Message should of type {typeof(T)}");
            }

            await SendMessageAsync(castedObject);
        }
    }

    #endregion

}
