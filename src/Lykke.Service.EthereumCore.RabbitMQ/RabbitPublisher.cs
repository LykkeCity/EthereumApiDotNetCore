using Common;
using Lykke.Job.EthereumCore.Contracts.Events;
using Lykke.RabbitMqBroker.Publisher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lykke.Job.EthereumCore.Contracts.Events.LykkePay;

namespace Lykke.Service.RabbitMQ
{
    public interface IRabbitQueuePublisher
    {
        Task PublshEvent(string rabbitEvent);

        Task PublshEvent<T>(T rabbitEvent);
    }

    public class RabbitQueuePublisher : IRabbitQueuePublisher
    {
        private IMessageProducer<string> _publisher;
        private readonly IMessageProducer<TransferEvent> _lykkePayPublisher;
        private readonly IMessageProducer<HotWalletEvent> _hotWalletPublisher;
        private readonly Dictionary<Type, MessageProducerWrapper> _messageProducerDictionary =
            new Dictionary<Type, MessageProducerWrapper>();

        public RabbitQueuePublisher(IMessageProducer<string> publisher,
            IMessageProducer<HotWalletEvent> hotWalletPublisher,
            IMessageProducer<TransferEvent> lykkePayPublisher)
        {
            _publisher = publisher;
            _hotWalletPublisher = hotWalletPublisher;
            _lykkePayPublisher = lykkePayPublisher;

            #region String

            MessageProducerWrapper strringWrapper = CreateWrapper(typeof(string), _publisher);
            _messageProducerDictionary.Add(typeof(string), strringWrapper);

            #endregion


            #region HotWalletCashoutEvent

            MessageProducerWrapper hotWalletCashoutEventWrapper = CreateWrapper(typeof(HotWalletEvent), _hotWalletPublisher);
            _messageProducerDictionary.Add(typeof(HotWalletEvent), hotWalletCashoutEventWrapper);

            #endregion

            #region LykkePayEvents

            MessageProducerWrapper lykkePayEventWrapper = CreateWrapper(typeof(TransferEvent), _lykkePayPublisher);
            _messageProducerDictionary.Add(typeof(TransferEvent), lykkePayEventWrapper);

            #endregion
        }

        public async Task PublshEvent(string rabbitEvent)
        {
            await _publisher.ProduceAsync(rabbitEvent);
        }

        public async Task PublshEvent<T>(T rabbitEvent)
        {
            _messageProducerDictionary.TryGetValue(typeof(T), out MessageProducerWrapper wrapper);

            if (wrapper == null)
            {
                throw new Exception("Message is of unsupported type");
            }

            await wrapper.SendMessageAsync(rabbitEvent);
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
