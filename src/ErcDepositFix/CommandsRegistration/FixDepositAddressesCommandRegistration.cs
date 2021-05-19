using ErcDepositFix.Commands;
using Microsoft.Extensions.CommandLineUtils;

namespace ErcDepositFix.CommandsRegistration
{
    [CommandRegistration("fix-deposit-addresses")]
    public class FixDepositAddressesCommandRegistration : ICommandRegistration
    {
        private readonly CommandFactory _factory;

        public FixDepositAddressesCommandRegistration(CommandFactory factory)
        {
            _factory = factory;
        }

        public void StartExecution(CommandLineApplication lineApplication)
        {
            lineApplication.Description = "This is the description for fix-deposit-addresses.";
            lineApplication.HelpOption("-?|-h|--help");

            var settingsUrlOption = lineApplication.Option("-s|--settings <optionvalue>",
                "ethereum core SettingsUrl",
                CommandOptionType.SingleValue);

            var csvFilePathOption = lineApplication.Option("-p|--path <optionvalue>",
                "ethereum core SettingsUrl",
                CommandOptionType.SingleValue);

            lineApplication.OnExecute(async () =>
            {
                var command = _factory.CreateCommand((helper) => new FixDepositAddressesCommand(helper,
                    settingsUrlOption.Value(),
                    csvFilePathOption.Value()));

                return await command.ExecuteAsync();
            });
        }
    }
}

