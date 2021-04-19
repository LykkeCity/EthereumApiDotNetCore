using DepositContractResolver.Commands;
using Microsoft.Extensions.CommandLineUtils;

namespace DepositContractResolver.CommandsRegistration
{
    [CommandRegistration("transfer-from-adapter")]
    public class TransferFromAdapterCommandRegistration : ICommandRegistration
    {
        private readonly CommandFactory _factory;

        public TransferFromAdapterCommandRegistration(CommandFactory factory)
        {
            _factory = factory;
        }

        public void StartExecution(CommandLineApplication lineApplication)
        {
            lineApplication.Description = "This is the description for transfer-from-adapter.";
            lineApplication.HelpOption("-?|-h|--help");

            var settingsUrlOption = lineApplication.Option("-s|--settings <optionvalue>",
                "ethereum core SettingsUrl",
                CommandOptionType.SingleValue);

            var coinAdapterOption =
                lineApplication.Option("-ca|--coin-adapter-address <optionvalue>",
                    "CoinAdapter is an address of ETH adapter contract",
                    CommandOptionType.SingleValue);
            var fromAddressOption =
                lineApplication.Option("-f|--from-address <optionvalue>",
                    "FromAddress is an address of user old(legacy) deposit address",
                    CommandOptionType.SingleValue);
            var toAddressOption =
                lineApplication.Option("-t|--to-address <optionvalue>",
                    "ToAddress is the user's address of ETH asset in blockchain integration layer",
                    CommandOptionType.SingleValue);

            lineApplication.OnExecute(async () =>
            {
                var command = _factory.CreateCommand((helper) => new TransferFromAdapterCommand(helper,
                    settingsUrlOption.Value(),
                    coinAdapterOption.Value(),
                    fromAddressOption.Value(),
                    toAddressOption.Value()));

                return await command.ExecuteAsync();
            });
        }
    }
}

