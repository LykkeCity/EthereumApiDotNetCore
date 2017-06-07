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

        public MonitoringTransferContracts(IBaseSettings settings,
            IErcInterfaceService ercInterfaceService,
            ITransferContractRepository transferContractsRepository,
            ILog logger,
            IPaymentService paymentService,
            IUserPaymentRepository userPaymentRepository,
            TransferContractService transferContractService,
            IUserTransferWalletRepository userTransferWalletRepository,
            ITransferContractTransactionService transferContractTransactionService,
            IEthereumTransactionService ethereumTransactionService
            )
        {
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
        }

        [TimerTrigger("0.00:04:00")]
        public async Task Execute()
        {
            await _transferContractsRepository.ProcessAllAsync(async (item) =>
            {
                try
                {
                    //Check that transfer contract assigned to user
                    if (!string.IsNullOrEmpty(item.UserAddress) && !string.IsNullOrEmpty(item.AssignmentHash))
                    {
                        var assignmentCompleted = await _ethereumTransactionService.IsTransactionExecuted(item.AssignmentHash, Constants.GasForCoinTransaction);
                        if (!assignmentCompleted)
                        {
                            //_ethereumTransactionService
                            throw new Exception($"User assignment wasa not completed for {item.UserAddress} (trHash:{item.AssignmentHash})");
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
                }
                catch (Exception e)
                {
                    await _logger.WriteErrorAsync("MonitoringTransferContracts", "Execute", "",e, DateTime.UtcNow);
                }
            });
        }
    }
}
