using Microsoft.Extensions.CommandLineUtils;

namespace PassTokenAddressExporter.CommandsRegistration
{
    public interface ICommandRegistration
    {
        void StartExecution(CommandLineApplication lineApplication);
    }
}
