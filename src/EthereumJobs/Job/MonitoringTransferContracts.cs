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

namespace EthereumJobs.Job
{
    public class MonitoringTransferContracts
    {
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

        public MonitoringTransferContracts(IBaseSettings settings,
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
            ISlackNotifier slackNotifier
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
        }

        [TimerTrigger("0.00:04:00")]
        public async Task Execute()
        {
            await _transferContractsRepository.ProcessAllAsync(async (item) =>
            {
                try
                {
                    //Check that transfer contract assigned to user
                    if (!string.IsNullOrEmpty(item.UserAddress))
                    {
                        var userAddress = await _transferContractService.GetUserAddressForTransferContract(item.ContractAddress);
                        if (string.IsNullOrEmpty(userAddress) || userAddress == Constants.EmptyEthereumAddress)
                        {
                            bool assignmentCompleted = false;
                            if (!string.IsNullOrEmpty(item.AssignmentHash))
                            {
                                assignmentCompleted = await _ethereumTransactionService.IsTransactionExecuted(item.AssignmentHash, Constants.GasForCoinTransaction);
                            }
                            if (!assignmentCompleted)
                            {
                                //await UpdateUserAssignmentFail(item.ContractAddress, item.UserAddress, item.CoinAdapterAddress);
                                await _logger.WriteWarningAsync("MonitoringTransferContracts", "Executr", $"User assignment was not completed for {item.UserAddress} (coinAdaptertrHash::{ item.CoinAdapterAddress}, trHash: { item.AssignmentHash})", "", DateTime.UtcNow);

                                throw new Exception($"User assignment was not completed for {item.UserAddress} (coinAdaptertrHash::{item.CoinAdapterAddress}, trHash: {item.AssignmentHash})");
                            }
                        }
                        //it is a transfer wallet
                        IUserTransferWallet wallet = await _userTransferWalletRepository.GetUserContractAsync(item.UserAddress, item.ContractAddress);
                        if (wallet == null ||
                            string.IsNullOrEmpty(wallet.LastBalance) ||
                            wallet.LastBalance == "0")
                        {
                            BigInteger balance = await _transferContractService.GetBalance(item.ContractAddress);

                            if (balance > 0)
                            {
                                await _userTransferWalletRepository.ReplaceAsync(new UserTransferWallet()
                                {
                                    LastBalance = balance.ToString(),
                                    TransferContractAddress = item.ContractAddress,
                                    UserAddress = item.UserAddress,
                                    UpdateDate = DateTime.UtcNow
                                });

                                await _transferContractTransactionService.PutContractTransferTransaction(new TransferContractTransaction()
                                {
                                    Amount = balance.ToString(),
                                    UserAddress = item.UserAddress,
                                    CoinAdapterAddress = item.CoinAdapterAddress,
                                    ContractAddress = item.ContractAddress,
                                    CreateDt = DateTime.UtcNow
                                });

                                string currency = item.ContainsEth ? "Wei" : "Tokens";
                                await _logger.WriteInfoAsync("MonitoringTransferContracts", "Execute", "", $"Balance on transfer address - {item.ContractAddress}" +
                                    $" for adapter contract {item.CoinAdapterAddress} is {balance} ({currency})" +
                                    $" transfer belongs to user {item.UserAddress}", DateTime.UtcNow);
                            }
                        }
                    }
                    //else
                    //{
                    //    await UpdateUserAssignmentFail(item.ContractAddress, item.UserAddress, item.CoinAdapterAddress);
                    //}
                }
                catch (Exception e)
                {
                    await _logger.WriteErrorAsync("MonitoringTransferContracts", "Execute", "", e, DateTime.UtcNow);
                }
            });
        }

        public async Task UpdateUserAssignmentFail(string contractAddress, string userAddress, string coinAdapter)
        {
            var canBeRestoredInternally = !string.IsNullOrEmpty(userAddress) && userAddress == Constants.EmptyEthereumAddress;
            var userAssignmentFail = await _userAssignmentFailRepository.GetAsync(contractAddress);

            if (userAssignmentFail == null)
            {
                userAssignmentFail = new UserAssignmentFail()
                {
                    CanBeRestoredInternally = canBeRestoredInternally,
                    ContractAddress = contractAddress,
                    FailCount = 0
                };
            }

            if (userAssignmentFail.FailCount == 5)
            {
                if (canBeRestoredInternally)
                {
                    var message = new TransferContractUserAssignment()
                    {
                        CoinAdapterAddress = coinAdapter,
                        TransferContractAddress = contractAddress,
                        UserAddress = userAddress
                    };

                    await _queueUserAssignment.PutRawMessageAsync(JsonConvert.SerializeObject(message));
                    userAssignmentFail.FailCount = 0;
                }
                else
                {
                    await _slackNotifier.ErrorAsync($"TransferAddress - {contractAddress}, UserAddress - {userAddress}, " +
                        $"CoinAdapter Address - {coinAdapter} can't be restored internally");
                }
            } else
            {
                userAssignmentFail.FailCount++;
            }

            if (canBeRestoredInternally)
            {
                await _userAssignmentFailRepository.SaveAsync(userAssignmentFail);
            }
        }
    }
}
