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
    public class ScanDepositAndWithdrawCommand : ICommand
    {
        private readonly IConfigurationHelper _helper;
        private readonly string _settingsUrl;

        public ScanDepositAndWithdrawCommand(IConfigurationHelper helper,
            string settingsUrl)
        {
            _helper = helper;
            _settingsUrl = settingsUrl;
        }

        public async Task<int> ExecuteAsync()
        {
            return await ScanDepositBalancesAndWithdrawAsync(_settingsUrl);
        }

        private async Task<int> ScanDepositBalancesAndWithdrawAsync(
            string settingsUrl)
        {
            #region RegisterDependencies

            var appSettings = _helper.GetCurrentSettingsFromUrl(settingsUrl);
            var (resolver, consoleLogger) = _helper.GetResolver(appSettings);

            #endregion

            bool isScanCompleted = false;
            string hotWalletAddress = appSettings.CurrentValue.Ethereum.HotwalletAddress;
            var transferContractRepository = resolver.Resolve<ITransferContractRepository>();
            var transferContractService = resolver.Resolve<ITransferContractService>();
            var exchangeContractService = resolver.Resolve<IExchangeContractService>();
            var contractService = resolver.Resolve<IContractService>();

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
                        await consoleLogger.WriteInfoAsync(nameof(ScanDepositBalancesAndWithdrawAsync),
                            depositContract.UserAddress, $" Balance on adapter is {adapterBalance}.");

                        if (depositContract.UserAddress.ToLower() == hotWalletAddress.ToLower())
                        {
                            Console.WriteLine("HOTWALLET");
                        }

                        if (depositContract.UserAddress.ToLower() != hotWalletAddress.ToLower())
                        {
                            var guid = Guid.NewGuid();
                            string transactionHashFromAdapter = await exchangeContractService.TransferWithoutSignCheck(guid,
                                depositContract.CoinAdapterAddress,
                                depositContract.UserAddress,
                                hotWalletAddress,
                                adapterBalance,
                                "01");

                            await consoleLogger.WriteInfoAsync(nameof(ScanDepositBalancesAndWithdrawAsync),
                                depositContract.UserAddress, $"Transfer from the adapter address to the hotwallet(segment) is pending. {transactionHashFromAdapter}");

                            await contractService.WaitForTransactionToCompleteAsync(transactionHashFromAdapter);

                            await consoleLogger.WriteInfoAsync(nameof(ScanDepositBalancesAndWithdrawAsync),
                                depositContract.UserAddress, $"Transfer to the destination address is completed. check:" +
                                             $"https://etherscan.io/tx/{transactionHashFromAdapter}");
                        }
                    }
                }

            } while (!string.IsNullOrEmpty(continuationToken));

            await consoleLogger.WriteInfoAsync(nameof(ScanDepositBalancesAndWithdrawAsync),
                "", $"Scanning is completed");

            return 0;
        }

    }
}
