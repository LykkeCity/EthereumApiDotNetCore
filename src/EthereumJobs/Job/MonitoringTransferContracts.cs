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
    public class MonitoringTransferContracts : TimerPeriod
    {

        private const int TimerPeriodSeconds = 60 * 3;
        private const int AlertNotChangedBalanceCount = 3;

        private readonly ILog _logger;
        private readonly IPaymentService _paymentService;
        private readonly IEmailNotifierService _emailNotifierService;
        private readonly ITransferContractRepository _transferContractsRepository;
        private readonly IBaseSettings _settings;
        private readonly IErcInterfaceService _ercInterfaceService;
        private readonly IUserPaymentRepository _userPaymentRepository;
        private readonly TransferContractService _transferContractService;
        private readonly IUserTransferWalletRepository _userTransferWalletRepository;
        private readonly ITransferContractTransactionService _transferContractTransactionService;

        public MonitoringTransferContracts(IBaseSettings settings,
            IErcInterfaceService ercInterfaceService,
            ITransferContractRepository transferContractsRepository,
            ILog logger,
            IPaymentService paymentService,
            IEmailNotifierService emailNotifierService,
            IUserPaymentRepository userPaymentRepository,
            TransferContractService transferContractService,
            IUserTransferWalletRepository userTransferWalletRepository,
            ITransferContractTransactionService transferContractTransactionService
            ) :
            base("MonitoringTransferContracts", TimerPeriodSeconds * 1000, logger)
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
            await _transferContractsRepository.ProcessAllAsync(async (item) =>
            {
                //it is a transfer wallet
                IUserTransferWallet wallet = await _userTransferWalletRepository.GetUserContractAsync(item.UserAddress, item.ContractAddress);
                if (wallet == null ||
                    string.IsNullOrEmpty(wallet.LastBalance) ||
                    wallet.LastBalance == "0")
                {
                    BigInteger balance;

                    if (!item.ContainsEth)
                    {
                        balance =
                        await _ercInterfaceService.GetBalanceForExternalTokenAsync(item.ContractAddress, item.ExternalTokenAddress);
                    }
                    else
                    {
                        balance = await _paymentService.GetTransferContractBalanceInWei(item.ContractAddress);
                    }

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
                    }
                }
            });
        }
    }
}
