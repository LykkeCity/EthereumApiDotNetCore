using System.Threading.Tasks;
using Core.Repositories;
using Core.Timers;
using Nethereum.Web3;
using Services;
using Common.Log;
using Core.Settings;
using System.Numerics;

namespace EthereumJobs.Job
{
    public class MonitoringTransferContracts : TimerPeriod
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

        public MonitoringTransferContracts(BaseSettings settings, 
            ErcInterfaceService ercInterfaceService,
            ITransferContractRepository transferContractsRepository,
            ILog logger,
            IPaymentService paymentService, 
            IEmailNotifierService emailNotifierService,
            IUserPaymentRepository userPaymentRepository) :
            base("MonitoringContractBalance", TimerPeriodSeconds * 1000, logger)
        {
            _ercInterfaceService = ercInterfaceService;
            _settings = settings;
            _transferContractsRepository = transferContractsRepository;
            _logger = logger;
            _paymentService = paymentService;
            _emailNotifierService = emailNotifierService;
            _userPaymentRepository = userPaymentRepository;
        }

        public override async Task Execute()
        {
            await _transferContractsRepository.ProcessAllAsync(async (item) =>
            {
                if (!item.ContainsEth)
                {
                    BigInteger balance =
                    await _ercInterfaceService.GetBalanceForExternalToken(item.ContractAddress, item.ExternalTokenAddress);
                } else
                {
                    decimal ethereumBalance = await _paymentService.GetUserContractBalance(item.ContractAddress);
                }

                //await _userPaymentRepository.SaveAsync();
            });

        }
    }
}
