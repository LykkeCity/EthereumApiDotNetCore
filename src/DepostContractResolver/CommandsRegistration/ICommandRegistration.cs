using Microsoft.Extensions.CommandLineUtils;

namespace DepositContractResolver.CommandsRegistration
{
    public interface ICommandRegistration
    {
        void StartExecution(CommandLineApplication lineApplication);
    }
}
