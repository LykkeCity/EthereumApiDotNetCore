using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Services
{
    public interface IShutdownManager
    {
        Task StopAsync();
    }
}