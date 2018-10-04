using System.Threading.Tasks;

namespace PassTokenAddressExporter.Commands
{
    public interface ICommand
    {
        Task<int> ExecuteAsync();
    }
}
