using System.Threading.Tasks;
using Autofac;
using Common;
using Lykke.Job.EthereumCore.Contract;

namespace Lykke.Job.EthereumCore.Core.Services
{
    public interface IMyRabbitPublisher : IStartable, IStopable
    {
        Task PublishAsync(MyPublishedMessage message);
    }
}