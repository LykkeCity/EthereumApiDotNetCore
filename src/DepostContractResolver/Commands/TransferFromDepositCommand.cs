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
    public class TransferFromDepositCommand : ICommand
    {
        private readonly IConfigurationHelper _helper;
        private readonly string _settingsUrl;
        private readonly string _fromAddress;
        private readonly string _toAddress;

        public TransferFromDepositCommand(IConfigurationHelper helper,
            string settingsUrl,
            string fromAddress,
            string toAddress)
        {
            _helper = helper;
            _settingsUrl = settingsUrl;
            _fromAddress = fromAddress;
            _toAddress = toAddress;
        }

        public async Task<int> ExecuteAsync()
        {
            return await TransferFromLegacyDepositToUsersEthDepositAsync(_settingsUrl, _fromAddress, _toAddress);
        }

        private async Task<int> TransferFromLegacyDepositToUsersEthDepositAsync(
            string settingsUrl,
            string fromAddress,
            string toAddress)
        {
            #region RegisterDependencies

            var appSettings = _helper.GetCurrentSettingsFromUrl(settingsUrl);
            var (resolver, consoleLogger) = _helper.GetResolver(appSettings);

            #endregion

            string depositContractAddress = fromAddress?.ToLower();
            var transferContractRepository = resolver.Resolve<ITransferContractRepository>();
            var transferContractService = resolver.Resolve<ITransferContractService>();
            var contractService = resolver.Resolve<IContractService>();
            var exchangeContractService = resolver.Resolve<IExchangeContractService>();
            var oldDeposiTransferContract = await transferContractRepository.GetAsync(depositContractAddress);

            if (oldDeposiTransferContract == null)
            {
                await consoleLogger.WriteInfoAsync(nameof(TransferFromLegacyDepositToUsersEthDepositAsync),
                    depositContractAddress, "Deposit contract does not exist");

                return 0;
            }

            BigInteger balance = await transferContractService.GetBalance(depositContractAddress); //ETH Balance in wei

            if (balance == 0)
            {
                await consoleLogger.WriteInfoAsync(nameof(TransferFromLegacyDepositToUsersEthDepositAsync),
                    depositContractAddress, "Deposit contract balance is 0");

                return 0;
            }

            var transactionHash = await transferContractService.RecievePaymentFromTransferContract(
                oldDeposiTransferContract.ContractAddress,
                oldDeposiTransferContract.CoinAdapterAddress);

            await consoleLogger.WriteInfoAsync(nameof(TransferFromLegacyDepositToUsersEthDepositAsync),
                depositContractAddress, $"Transfer from deposit address to adapter is pending. {transactionHash}");

            await contractService.WaitForTransactionToCompleteAsync(transactionHash);

            BigInteger adapterBalance = await transferContractService.GetBalanceOnAdapter(
                oldDeposiTransferContract.CoinAdapterAddress,
                oldDeposiTransferContract.UserAddress,
                checkInPendingBlock: true);

            var guid = Guid.NewGuid();
            string transactionHashFromAdapter = await exchangeContractService.CashOutWithoutSignCheck(guid,
                oldDeposiTransferContract.CoinAdapterAddress,
                oldDeposiTransferContract.UserAddress,
                toAddress,
                balance);

            await consoleLogger.WriteInfoAsync(nameof(TransferFromLegacyDepositToUsersEthDepositAsync),
                depositContractAddress, $"Transfer from the adapter address to the destination is pending. {transactionHashFromAdapter}");

            await contractService.WaitForTransactionToCompleteAsync(transactionHashFromAdapter);

            await consoleLogger.WriteInfoAsync(nameof(TransferFromLegacyDepositToUsersEthDepositAsync),
                depositContractAddress, $"Transfer to the destination address is completed. check:" +
                                        $"https://etherscan.io/tx/{transactionHashFromAdapter}");

            return 0;
        }
    }
}
