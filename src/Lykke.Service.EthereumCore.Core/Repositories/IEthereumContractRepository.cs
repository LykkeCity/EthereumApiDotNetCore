using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface INewEthereumContract
    {
        string Abi { get; set; }
        string ByteCode { get; set; }
    }
    public interface IEthereumContract : INewEthereumContract
    {
        string ContractAddress { get; set; }
    }

    public class EthereumContract : IEthereumContract
    {
        public string ContractAddress { get; set; }
        public string Abi { get; set; }
        public string ByteCode { get; set; }
    }

    public interface IEthereumContractRepository
    {
        Task SaveAsync(IEthereumContract transferContract);
        Task<IEthereumContract> GetAsync(string contractAddress);
    }
}
