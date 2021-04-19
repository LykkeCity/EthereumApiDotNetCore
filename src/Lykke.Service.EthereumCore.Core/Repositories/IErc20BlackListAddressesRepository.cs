using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface IErc20BlackListAddress
    {
        string Address { get; set; }
    }

    public class Erc20BlackListAddress : IErc20BlackListAddress
    {
        public string Address { get; set; }
    }

    public interface IErc20BlackListAddressesRepository
    {
        Task SaveAsync(IErc20BlackListAddress address);
        Task<IErc20BlackListAddress> GetAsync(string address);
        Task DeleteAsync(string address);
    }
}
