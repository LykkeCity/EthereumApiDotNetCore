using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Common
{
    public interface IAggregatedErc20DepositContractLocatorService
    {
        Task<(bool, WorkflowType)> ContainsWithTypeAsync(string address);
    }
}
