using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface IWhiteListAddress
    {
        string Address { get; set; }
    }

    public class WhiteListAddress : IWhiteListAddress
    {
        public string Address { get; set; }
    }

    public interface IWhiteListAddressesRepository
    {
        Task SaveAsync(IWhiteListAddress address);
        Task<IWhiteListAddress> GetAsync(string address);
        Task DeleteAsync(string address);
    }
}
