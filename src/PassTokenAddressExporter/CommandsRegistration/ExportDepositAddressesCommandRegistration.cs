using Microsoft.Extensions.CommandLineUtils;
using PassTokenAddressExporter.Commands;

namespace PassTokenAddressExporter.CommandsRegistration
{
    [CommandRegistration("export-deposit-addresses")]
    public class ExportDepositAddressesCommandRegistration : ICommandRegistration
    {
        private readonly CommandFactory _factory;

        public ExportDepositAddressesCommandRegistration(CommandFactory factory)
        {
            _factory = factory;
        }

        public void StartExecution(CommandLineApplication lineApplication)
        {
            lineApplication.Description = "This is the description for export-deposit-addresses.";
            lineApplication.HelpOption("-?|-h|--help");

            var settingsUrlOption = lineApplication.Option("-s|--settings <optionvalue>",
                "ethereum core SettingsUrl",
                CommandOptionType.SingleValue);

            lineApplication.OnExecute(async () =>
            {
                var command = _factory.CreateCommand((helper) => new ExportDepositAddressesCommand(helper,
                    settingsUrlOption.Value()));

                return await command.ExecuteAsync();
            });
        }
    }
}

