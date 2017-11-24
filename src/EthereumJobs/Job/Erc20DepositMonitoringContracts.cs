using System.Threading.Tasks;
using Core.Repositories;
using Nethereum.Web3;
using Services;
using Common.Log;
using Core.Settings;
using System.Numerics;
using System;
using Common;
using Lykke.JobTriggers.Triggers.Attributes;
using Core;
using Nethereum.Util;
using AzureStorage.Queue;
using Newtonsoft.Json;
using Core.Notifiers;
using Lykke.Service.Assets.Client;
using System.Linq;
using System.Collections;
using Lykke.Service.Assets.Client.Models;
using System.Collections.Generic;
using EthereumSamuraiApiCaller;
using Services.Erc20;

namespace EthereumJobs.Job
{
    public class Erc20DepositMonitoringContracts
    {
        private const int _attempsBeforeReassign = 20;
        private readonly ILog _logger;
        private readonly IPaymentService _paymentService;
        private readonly ITransferContractRepository _transferContractsRepository;
        private readonly IBaseSettings _settings;
        private readonly IErcInterfaceService _ercInterfaceService;
        private readonly IUserPaymentRepository _userPaymentRepository;
        private readonly TransferContractService _transferContractService;
        private readonly IUserTransferWalletRepository _userTransferWalletRepository;
        private readonly ITransferContractTransactionService _transferContractTransactionService;
        private readonly IEthereumTransactionService _ethereumTransactionService;
        private readonly AddressUtil _util;
        private readonly ITransferContractUserAssignmentQueueService _transferContractUserAssignmentQueueService;
        private readonly IUserAssignmentFailRepository _userAssignmentFailRepository;
        private readonly IQueueExt _queueUserAssignment;
        private readonly ISlackNotifier _slackNotifier;
        private readonly IErc20DepositContractService _erc20DepositContractService;
        private readonly IAssetsService _assetsService;
        private readonly IErc20BalanceService _erc20BalanceService;
        private readonly IErc20DepositTransactionService _erc20DepositTransactionService;

        public Erc20DepositMonitoringContracts(IBaseSettings settings,
            IErcInterfaceService ercInterfaceService,
            ITransferContractRepository transferContractsRepository,
            ILog logger,
            IPaymentService paymentService,
            IUserPaymentRepository userPaymentRepository,
            TransferContractService transferContractService,
            IUserTransferWalletRepository userTransferWalletRepository,
            ITransferContractTransactionService transferContractTransactionService,
            IEthereumTransactionService ethereumTransactionService,
            ITransferContractUserAssignmentQueueService transferContractUserAssignmentQueueService,
            IUserAssignmentFailRepository userAssignmentFailRepository,
            IQueueFactory queueFactory,
            ISlackNotifier slackNotifier,
            IErc20DepositContractService erc20DepositContractService,
            IAssetsService assetsService,
            IErc20BalanceService erc20BalanceService,
            IErc20DepositTransactionService erc20DepositTransactionService
            )
        {
            _util = new AddressUtil();
            _ethereumTransactionService = ethereumTransactionService;
            _ercInterfaceService = ercInterfaceService;
            _settings = settings;
            _transferContractsRepository = transferContractsRepository;
            _logger = logger;
            _paymentService = paymentService;
            _userPaymentRepository = userPaymentRepository;
            _transferContractService = transferContractService;
            _userTransferWalletRepository = userTransferWalletRepository;
            _transferContractTransactionService = transferContractTransactionService;
            _transferContractUserAssignmentQueueService = transferContractUserAssignmentQueueService;
            _userAssignmentFailRepository = userAssignmentFailRepository;
            _queueUserAssignment = queueFactory.Build(Constants.TransferContractUserAssignmentQueueName);
            _slackNotifier = slackNotifier;
            _erc20DepositContractService = erc20DepositContractService;
            _assetsService = assetsService;
            _erc20BalanceService = erc20BalanceService;
            _erc20DepositTransactionService = erc20DepositTransactionService;
        }

        [TimerTrigger("0.00:04:00")]
        public async Task Execute()
        {
            IList<Erc20Token> erc20Tokens = null;

            try
            {
                var tradeableAssets = await _assetsService.AssetGetAllAsync();
                var supportedTokenAssets = tradeableAssets.Where(asset =>
                asset.Type == Lykke.Service.Assets.Client.Models.AssetType.Erc20Token && asset.IsTradable);
                var assetIds = supportedTokenAssets.Select(x => x.Id);
                var tradableTokens = await _assetsService.Erc20TokenGetBySpecificationAsync(new Lykke.Service.Assets.Client.Models.Erc20TokenSpecification()
                {
                    Ids = assetIds.ToList(),
                });

                erc20Tokens = tradableTokens?.Items;

            }
            catch (Exception exc)
            {
                await _logger.WriteErrorAsync(nameof(Erc20DepositMonitoringContracts),
                    nameof(Execute),
                    "Assets Service unavailable",
                    exc,
                    DateTime.UtcNow);

                return;
            }

            if (erc20Tokens != null && !erc20Tokens.Any())
            {
                await _logger.WriteWarningAsync(nameof(Erc20DepositMonitoringContracts),
                    nameof(Execute),
                    "",
                    "No tokens available for trade",
                    DateTime.UtcNow);

                return;
            }

            await _erc20DepositContractService.ProcessAllAsync(async (item) =>
            {
                try
                {
                    //Check that deposit contract assigned to user
                    if (!string.IsNullOrEmpty(item.UserAddress))
                    {
                        // null - means we ask for all balances on current address
                        var tokenBalances = await _erc20BalanceService.GetBalancesForAddress(item.ContractAddress, new string[0]);

                        if (tokenBalances != null)
                        {
                            foreach (var tokenBalance in tokenBalances)
                            {
                                string tokenAddress = tokenBalance.Erc20TokenAddress?.ToLower();
                                string formattedAddress =
                                _userTransferWalletRepository.FormatAddressForErc20(item.ContractAddress, tokenAddress);
                                IUserTransferWallet wallet =
                                await _userTransferWalletRepository.GetUserContractAsync(item.UserAddress, formattedAddress);
                                if (wallet == null ||
                                    string.IsNullOrEmpty(wallet.LastBalance) ||
                                    wallet.LastBalance == "0")
                                {
                                    BigInteger balance =
                                    await _ercInterfaceService.GetBalanceForExternalTokenAsync(item.ContractAddress, tokenAddress);

                                    if (balance > 0)
                                    {
                                        await _userTransferWalletRepository.ReplaceAsync(new UserTransferWallet()
                                        {
                                            LastBalance = balance.ToString(),
                                            TransferContractAddress = formattedAddress,
                                            UserAddress = item.UserAddress,
                                            UpdateDate = DateTime.UtcNow
                                        });

                                        await _erc20DepositTransactionService.PutContractTransferTransaction(new Erc20DepositContractTransaction()
                                        {
                                            Amount = balance.ToString(),
                                            UserAddress = item.UserAddress,
                                            TokenAddress = tokenAddress,
                                            ContractAddress = item.ContractAddress,
                                            CreateDt = DateTime.UtcNow,
                                        });

                                        await _logger.WriteInfoAsync(nameof(Erc20DepositMonitoringContracts),
                                            nameof(Execute), "", $"Balance on transfer address - {item.ContractAddress} is {balance} (Tokens of {tokenBalance.Erc20TokenAddress})" +
                                            $" transfer belongs to user {item.UserAddress}", DateTime.UtcNow);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        //TODO
                        //Notify That deposit contract does not have a user;
                    }
                }
                catch (Exception e)
                {
                    await _logger.WriteErrorAsync(nameof(Erc20DepositMonitoringContracts),
                                        nameof(Execute), "", e, DateTime.UtcNow);
                }
            });
        }
    }
}
