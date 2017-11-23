using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IErc20DepositContractRepository
    {
        Task AddOrReplace(string contractAddress, string userAddress);

        Task<string> Get(string userAddress);

        Task<IEnumerable<string>> GetAll();
    }
}