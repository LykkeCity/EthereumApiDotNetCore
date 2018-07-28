using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using DepositContractResolver.Helpers;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.EthereumCore.Services.Coins;

namespace DepositContractResolver.Commands
{
    public class TransferFromAdapterCommand : ICommand
    {
        private readonly IConfigurationHelper _helper;
        private readonly string _settingsUrl;
        private readonly string _coinAdapter;
        private readonly string _fromAddress;
        private readonly string _toAddress;

        public TransferFromAdapterCommand(IConfigurationHelper helper,
            string settingsUrl,
            string coinAdapter,
            string fromAddress,
            string toAddress)
        {
            _helper = helper;
            _settingsUrl = settingsUrl;
            _coinAdapter = coinAdapter;
            _fromAddress = fromAddress;
            _toAddress = toAddress;
        }

        public async Task<int> ExecuteAsync()
        {
            return await TransferFromAdapterToUsersEthDepositAsync(_settingsUrl, _coinAdapter, _fromAddress, _toAddress);
        }

        private async Task<int> TransferFromAdapterToUsersEthDepositAsync(
            string settingsUrl,
            string coinAdapter,
            string fromAddress,
            string toAddress)
        {
            #region RegisterDependencies

            var appSettings = _helper.GetCurrentSettingsFromUrl(settingsUrl);
            var (resolver, consoleLogger) = _helper.GetResolver(appSettings);

            #endregion

            var transferContractService = resolver.Resolve<ITransferContractService>();
            var contractService = resolver.Resolve<IContractService>();
            var exchangeContractService = resolver.Resolve<IExchangeContractService>();

            BigInteger adapterBalance = await transferContractService.GetBalanceOnAdapter(
                coinAdapter,
                fromAddress,
                checkInPendingBlock: true);

            if (adapterBalance == 0)
            {
                await consoleLogger.WriteInfoAsync(nameof(TransferFromAdapterToUsersEthDepositAsync),
                    fromAddress, "Adapter balance is 0");

                return 0;
            }

            var guid = Guid.NewGuid();
            string transactionHashFromAdapter = await exchangeContractService.CashOutWithoutSignCheck(guid,
                coinAdapter,
                fromAddress,
                toAddress,
                adapterBalance);

            await consoleLogger.WriteInfoAsync(nameof(TransferFromAdapterToUsersEthDepositAsync),
                fromAddress, $"Transfer from the adapter address to the destination is pending. {transactionHashFromAdapter}");

            await contractService.WaitForTransactionToCompleteAsync(transactionHashFromAdapter);

            await consoleLogger.WriteInfoAsync(nameof(TransferFromAdapterToUsersEthDepositAsync),
                fromAddress, $"Transfer to the destination address is completed. check:" +
                                        $"https://etherscan.io/tx/{transactionHashFromAdapter}");

            return 0;
        }
    }
}
