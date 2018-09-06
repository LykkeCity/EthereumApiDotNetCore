using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Autofac;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.EthereumCore.Services.Coins;
using PassTokenAddressExporter.Helpers;

namespace PassTokenAddressExporter.Commands
{
    public class ExportDepositAddressesCommand : ICommand
    {
        private readonly IConfigurationHelper _helper;
        private readonly string _settingsUrl;

        public ExportDepositAddressesCommand(IConfigurationHelper helper,
            string settingsUrl)
        {
            _helper = helper;
            _settingsUrl = settingsUrl;
        }

        public async Task<int> ExecuteAsync()
        {
            return await ExportDepositAddressesAsync(_settingsUrl);
        }

        private async Task<int> ExportDepositAddressesAsync(
            string settingsUrl)
        {
            #region RegisterDependencies

            var appSettings = _helper.GetCurrentSettingsFromUrl(settingsUrl);
            var (resolver, consoleLogger) = _helper.GetResolver(appSettings);

            #endregion

            string hotWalletAddress = appSettings.CurrentValue.Ethereum.HotwalletAddress;
            var transferContractRepository = 
                (IErc223DepositContractRepository)resolver.ResolveKeyed(Constants.DefaultKey, 
                    typeof(IErc223DepositContractRepository));

            string continuationToken = null;

            await consoleLogger.WriteInfoAsync(nameof(ExportDepositAddressesAsync),
                "", $"Begin export");

            using (var writer = new StreamWriter("exportedDeposits.csv"))
            using (var csvWriter = new CsvHelper.CsvWriter(writer))
            {
                csvWriter.WriteHeader<DepositRecord>();
                csvWriter.NextRecord();

                var record = new DepositRecord { Address = hotWalletAddress };
                csvWriter.WriteRecord(record);
                csvWriter.NextRecord();

                do
                {
                    var (collection, token) = await transferContractRepository.GetByTokenAsync(100, continuationToken);
                    continuationToken = token;

                    foreach (var depositContract in collection)
                    {
                        var newRecord = new DepositRecord { Address = depositContract.ContractAddress };
                        csvWriter.WriteRecord(newRecord);
                        csvWriter.NextRecord();
                    }

                } while (!string.IsNullOrEmpty(continuationToken));
            }

            await consoleLogger.WriteInfoAsync(nameof(ExportDepositAddressesAsync),
                "", $"Export is completed");

            return 0;
        }

    }

    internal class DepositRecord
    {
        public string Address { get; set; }
    }
}
