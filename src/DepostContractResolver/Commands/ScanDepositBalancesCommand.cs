using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using DepositContractResolver.Helpers;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Utils;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.EthereumCore.Services.Coins;

namespace DepositContractResolver.Commands
{
    public class ScanDepositBalancesCommand : ICommand
    {
        private readonly IConfigurationHelper _helper;
        private readonly string _settingsUrl;
        private readonly string _coinAdapter;
        private readonly string _fromAddress;
        private readonly string _toAddress;

        public ScanDepositBalancesCommand(IConfigurationHelper helper,
            string settingsUrl)
        {
            _helper = helper;
            _settingsUrl = settingsUrl;
        }

        public async Task<int> ExecuteAsync()
        {
            return await ScanDepositBalancesAsync(_settingsUrl);
        }

        private async Task<int> ScanDepositBalancesAsync(
            string settingsUrl)
        {
            #region RegisterDependencies

            var appSettings = _helper.GetCurrentSettingsFromUrl(settingsUrl);
            var (resolver, consoleLogger) = _helper.GetResolver(appSettings);

            #endregion

            bool isScanCompleted = false;
            var transferContractRepository = resolver.Resolve<ITransferContractRepository>();
            var transferContractService = resolver.Resolve<ITransferContractService>();
            string continuationToken = null;

            do
            {
                var (collection, token) = await transferContractRepository.GetByTokenAsync(100, continuationToken);
                continuationToken = token;

                foreach (var depositContract in collection)
                {
                    if (string.IsNullOrEmpty(depositContract.UserAddress))
                        continue;

                    BigInteger adapterBalance = await transferContractService.GetBalanceOnAdapter(
                        depositContract.CoinAdapterAddress,
                        depositContract.UserAddress,
                        checkInPendingBlock: true);

                    if (adapterBalance != 0)
                    {
                        await consoleLogger.WriteInfoAsync(nameof(ScanDepositBalancesAsync),
                            depositContract.UserAddress, $" Balance om adapter is {adapterBalance}");
                    }
                }

            } while (!string.IsNullOrEmpty(continuationToken));

            await consoleLogger.WriteInfoAsync(nameof(ScanDepositBalancesAsync),
                "", $"Scanning is completed");

            return 0;
        }

    }
}
