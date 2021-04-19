using System.Threading.Tasks;

namespace DepositContractResolver.Commands
{
    public interface ICommand
    {
        Task<int> ExecuteAsync();
    }
}
