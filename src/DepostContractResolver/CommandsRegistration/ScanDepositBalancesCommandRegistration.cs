using System;
using System.Collections.Generic;
using System.Text;
using DepositContractResolver.Commands;
using Microsoft.Extensions.CommandLineUtils;

namespace DepositContractResolver.CommandsRegistration
{
    [CommandRegistration("scan-deposits")]
    public class ScanDepositBalancesCommandRegistration : ICommandRegistration
    {
        private readonly CommandFactory _factory;

        public ScanDepositBalancesCommandRegistration(CommandFactory factory)
        {
            _factory = factory;
        }

        public void StartExecution(CommandLineApplication lineApplication)
        {
            lineApplication.Description = "This is the description for scan-deposits.";
            lineApplication.HelpOption("-?|-h|--help");

            var settingsUrlOption = lineApplication.Option("-s|--settings <optionvalue>",
                "ethereum core SettingsUrl",
                CommandOptionType.SingleValue);

            lineApplication.OnExecute(async () =>
            {
                var command = _factory.CreateCommand((helper) => new ScanDepositBalancesCommand(helper,
                    settingsUrlOption.Value()));

                return await command.ExecuteAsync();
            });
        }
    }
}

