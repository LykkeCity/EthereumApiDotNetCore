using System.Threading.Tasks;

namespace ErcDepositFix.Commands
{
    public interface ICommand
    {
        Task<int> ExecuteAsync();
    }
}
