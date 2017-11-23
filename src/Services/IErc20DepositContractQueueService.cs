using System.Threading.Tasks;

namespace Services
{
    public interface IErc20DepositContractQueueService
    {
        Task<string> GetContractAddress();

        Task PushContractAddress(string contractAddress);

        Task<int> Count();
    }
}