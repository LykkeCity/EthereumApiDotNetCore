using DepositContractResolver.Commands;
using Microsoft.Extensions.CommandLineUtils;

namespace DepositContractResolver.CommandsRegistration
{
    [CommandRegistration("transfer-from-deposit")]
    public class TransferFromDepositCommandRegistration : ICommandRegistration
    {
        private readonly CommandFactory _factory;

        public TransferFromDepositCommandRegistration(CommandFactory factory)
        {
            _factory = factory;
        }

        public void StartExecution(CommandLineApplication lineApplication)
        {
            lineApplication.Description = "This is the description for transfer-from-deposit.";
            lineApplication.HelpOption("-?|-h|--help");

            var settingsUrlOption = lineApplication.Option("-s | --settings <settings>",
                "ethereum core SettingsUrl",
                CommandOptionType.SingleValue);
            var fromAddressOption =
                lineApplication.Option("-f | --from-address <fromaddress>",
                    "FromAddress is an address of user old(legacy) deposit address",
                    CommandOptionType.SingleValue);
            var toAddressOption =
                lineApplication.Option("-t | --to-address <toaddress>",
                    "ToAddress is the user's address of ETH asset in blockchain integration layer",
                    CommandOptionType.SingleValue);

            lineApplication.OnExecute(async () =>
            {
                var command = _factory.CreateCommand((helper) => new TransferFromDepositCommand(helper,
                    settingsUrlOption.Value(),
                    fromAddressOption.Value(),
                    toAddressOption.Value()));

                return await command.ExecuteAsync();
            });
        }
    }
}

