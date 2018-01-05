using System.Threading.Tasks;

namespace Lykke.Job.EthereumCore.Core.Services
{
    public interface IStartupManager
    {
        Task StartAsync();
    }
}