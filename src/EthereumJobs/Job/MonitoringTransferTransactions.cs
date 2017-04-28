using System.Threading.Tasks;
using Core.Repositories;
using Core.Timers;
using Nethereum.Web3;
using Services;
using Common.Log;
using Core.Settings;
using System.Numerics;
using System;

namespace EthereumJobs.Job
{
    public class MonitoringTransferTransactions : TimerPeriod
    {

        private const int TimerPeriodSeconds = 60 * 30;
        private const int AlertNotChangedBalanceCount = 3;

        private readonly ILog _logger;
        private readonly IPaymentService _paymentService;
        private readonly IEmailNotifierService _emailNotifierService;
        private readonly ITransferContractRepository _transferContractsRepository;
        private readonly BaseSettings _settings;
        private readonly ErcInterfaceService _ercInterfaceService;
        private readonly IUserPaymentRepository _userPaymentRepository;
        private readonly TransferContractService _transferContractService;
        private readonly IUserTransferWalletRepository _userTransferWalletRepository;
        private readonly ITransferContractTransactionService _transferContractTransactionService;

        public MonitoringTransferTransactions(BaseSettings settings,
            ErcInterfaceService ercInterfaceService,
            ITransferContractRepository transferContractsRepository,
            ILog logger,
            IPaymentService paymentService,
            IEmailNotifierService emailNotifierService,
            IUserPaymentRepository userPaymentRepository,
            TransferContractService transferContractService,
            IUserTransferWalletRepository userTransferWalletRepository,
            ITransferContractTransactionService transferContractTransactionService
            ) :
            base("MonitoringTransferTransactions", TimerPeriodSeconds * 1000, logger)
        {
            _ercInterfaceService = ercInterfaceService;
            _settings = settings;
            _transferContractsRepository = transferContractsRepository;
            _logger = logger;
            _paymentService = paymentService;
            _emailNotifierService = emailNotifierService;
            _userPaymentRepository = userPaymentRepository;
            _transferContractService = transferContractService;
            _userTransferWalletRepository = userTransferWalletRepository;
            _transferContractTransactionService = transferContractTransactionService;
        }

        public override async Task Execute()
        {
            try
            {
                while (await _transferContractTransactionService.CompleteTransfer() && Working)
                {
                }
            }
            catch (Exception ex)
            {
                await _logger.WriteErrorAsync("EthereumJob", "MonitoringTransferTransactions", "", ex);
            }
        }
    }
}
