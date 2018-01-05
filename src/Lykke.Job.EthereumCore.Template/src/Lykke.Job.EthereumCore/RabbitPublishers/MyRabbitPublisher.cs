using System.Threading.Tasks;
using Common.Log;
using Lykke.Job.EthereumCore.Contract;
using Lykke.Job.EthereumCore.Core.Services;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.Job.EthereumCore.RabbitPublishers
{
    public class MyRabbitPublisher : IMyRabbitPublisher
    {
        private readonly ILog _log;
        private readonly string _connectionString;
        private RabbitMqPublisher<MyPublishedMessage> _publisher;

        public MyRabbitPublisher(ILog log, string connectionString)
        {
            _log = log;
            _connectionString = connectionString;
        }

        public void Start()
        {
            // NOTE: Read https://github.com/LykkeCity/Lykke.RabbitMqDotNetBroker/blob/master/README.md to learn
            // about RabbitMq subscriber configuration

            var settings = RabbitMqSubscriptionSettings
                .CreateForPublisher(_connectionString, "ethereumcore");
            // TODO: Make additional configuration, using fluent API here:
            // ex: .MakeDurable()

            _publisher = new RabbitMqPublisher<MyPublishedMessage>(settings)
                .SetSerializer(new JsonMessageSerializer<MyPublishedMessage>())
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(settings))
                .PublishSynchronously()
                .SetLogger(_log)
                .Start();
        }

        public void Dispose()
        {
            _publisher?.Dispose();
        }

        public void Stop()
        {
            _publisher?.Stop();
        }

        public async Task PublishAsync(MyPublishedMessage message)
        {
            await _publisher.ProduceAsync(message);
        }
    }
}
