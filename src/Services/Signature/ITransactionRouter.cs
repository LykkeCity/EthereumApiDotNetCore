using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Services.Signature
{
    public interface ITransactionRouter
    {
        Task<string> GetNextSenderAddressAsync();
    }
}