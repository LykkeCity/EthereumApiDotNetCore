using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using ErcDepositFix.Csv;
using ErcDepositFix.Helpers;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Services;

namespace ErcDepositFix.Commands
{
    public class FixDepositAddressesCommand : ICommand
    {
        private readonly IConfigurationHelper _helper;
        private readonly string _settingsUrl;
        private readonly string _pathToEtherscanCsv;

        public FixDepositAddressesCommand(IConfigurationHelper helper,
            string settingsUrl,
            string pathToEtherscanCsv)
        {
            _helper = helper;
            _settingsUrl = settingsUrl;
            _pathToEtherscanCsv = pathToEtherscanCsv;
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

            var transferContractRepository = 
                (IErc223DepositContractRepository)resolver.ResolveKeyed(Constants.DefaultKey, 
                    typeof(IErc223DepositContractRepository));

            var oldTransferContractRepository =
                (IErc223DepositContractRepository)resolver.ResolveKeyed(Constants.DefaultKey,
                    typeof(IErc223DepositContractRepository));

            var poolFactory = resolver.Resolve< IErc20DepositContractQueueServiceFactory> ();
            var pool = poolFactory.Get(Constants.Erc20DepositContractPoolQueue);
            var ethereumContractPoolRepository = resolver.Resolve<IEthereumContractPoolRepository>();
            
            var existingContracts = new HashSet<string>();


            string continuationToken = null;

            do
            {
                var (collection, token) = await transferContractRepository.GetByTokenAsync(100, continuationToken);
                continuationToken = token;

                foreach (var depositContract in collection)
                {
                    existingContracts.Add(depositContract.ContractAddress.ToLowerInvariant());
                }

            } while (!string.IsNullOrEmpty(continuationToken));

            continuationToken = null;

            do
            {
                var (collection, token) = await oldTransferContractRepository.GetByTokenAsync(100, continuationToken);
                continuationToken = token;

                foreach (var depositContract in collection)
                {
                    existingContracts.Add(depositContract.ContractAddress.ToLowerInvariant());
                }

            } while (!string.IsNullOrEmpty(continuationToken));


            await consoleLogger.WriteInfoAsync(nameof(ExportDepositAddressesAsync),
                "", $"Begin fix");

            using (var reader = new StreamReader(_pathToEtherscanCsv))
            using (var csvReader = new CsvHelper.CsvReader(reader))
            {
                var transactions = csvReader.GetRecords<EtherscanTx>();

                foreach (var transaction in transactions)
                {
                    if (!string.IsNullOrEmpty(transaction.To) ||
                        string.IsNullOrEmpty(transaction.ContractAddress))
                        continue;

                    var contractAddress = transaction.ContractAddress.ToLowerInvariant();

                    if (existingContracts.Contains(contractAddress))
                        continue;

                    var isCreated = await ethereumContractPoolRepository.GetOrDefaultAsync(contractAddress);

                    if (!isCreated)
                    {
                        await ethereumContractPoolRepository.InsertOrReplaceAsync(contractAddress);
                        await pool.PushContractAddress(contractAddress);
                    }
                }
            }

            await consoleLogger.WriteInfoAsync(nameof(ExportDepositAddressesAsync),
                "", $"Fix is completed");

            return 0;
        }

    }

    internal class DepositRecord
    {
        public string Address { get; set; }
    }
}
