using Microsoft.Extensions.CommandLineUtils;

namespace ErcDepositFix.CommandsRegistration
{
    public interface ICommandRegistration
    {
        void StartExecution(CommandLineApplication lineApplication);
    }
}
