using System.Threading.Tasks;

namespace Lykke.Job.EthereumCore.Core.Services
{
    public interface IShutdownManager
    {
        Task StopAsync();
    }
}