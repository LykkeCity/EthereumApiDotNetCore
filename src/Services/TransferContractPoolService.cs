using Common.Log;
using Core;
using Core.Notifiers;
using Core.Repositories;
using Core.Settings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public class TransferContractPoolService
    {
        private readonly ITransferContractQueueServiceFactory _transferContractQueueServiceFactory;
        private readonly IContractService _contractService;
        private readonly IPaymentService _paymentService;
        private readonly ILog _logger;
        private readonly IBaseSettings _settings;
        private readonly ISlackNotifier _slackNotifier;
        private static DateTime _lastWarningSentTime = DateTime.MinValue;
        private readonly ITransferContractService _transferContractService;
        private readonly ICoinRepository _coinRepository;

        public TransferContractPoolService(ITransferContractQueueServiceFactory transferContractQueueServiceFactory,
            ITransferContractService transferContractService,
            IBaseSettings settings,
            IContractService contractService,
            IPaymentService paymentService,
            ISlackNotifier slackNotifier,
            ICoinRepository coinRepository,
            ILog logger)
        {
            _coinRepository = coinRepository;
            _transferContractService = transferContractService;
            _transferContractQueueServiceFactory = transferContractQueueServiceFactory;
            _settings = settings;
            _contractService = contractService;
            _paymentService = paymentService;
            _slackNotifier = slackNotifier;
            _logger = logger;
        }

        public async Task Execute(string coinAdapterAddress)
        {
            ICoin coin = await _coinRepository.GetCoinByAddress(coinAdapterAddress);

            if (coin == null)
            {
                throw new Exception($"Coin with addres {coinAdapterAddress} does not exist");
            }

            await Execute(coin);
        }

        public async Task Execute(ICoin coin)
        {
            await InternalBalanceCheck();

            string coinPoolQueueName = QueueHelper.GenerateQueueNameForContractPool(coin.AdapterAddress);
            ITransferContractQueueService transferContractQueueService = 
                _transferContractQueueServiceFactory.Get(coinPoolQueueName);

            int currentCount = await transferContractQueueService.Count();

            if (currentCount < _settings.MinContractPoolLength)
            {
                while (currentCount < _settings.MaxContractPoolLength)
                {
                    await InternalBalanceCheck();

                    List<string> trHashes = new List<string>(_settings.ContractsPerRequest);

                    for (int i = 0; i < _settings.ContractsPerRequest; i++)
                    {
                        var transferContractTrHash = 
                            await _transferContractService.CreateTransferContractTrHashWithoutUser(coin.AdapterAddress);
                        trHashes.Add(transferContractTrHash);
                    }

                    IEnumerable<string> contractAddresses = await _contractService.GetContractsAddresses(trHashes);
                    List<Task> contractPushTasks = new List<Task>();

                    foreach (var address in contractAddresses)
                    {
                        await transferContractQueueService.PushContract(new TransferContract()
                        {
                            CoinAdapterAddress = coin.AdapterAddress,
                            ContainsEth = coin.ContainsEth,
                            ContractAddress = address,
                            ExternalTokenAddress = coin.ExternalTokenAddress,
                        });
                    }

                    currentCount += _settings.ContractsPerRequest;
                }
            }
        }

        private async Task InternalBalanceCheck()
        {
            try
            {
                var balance = await _paymentService.GetMainAccountBalance();
                if (balance < _settings.MainAccountMinBalance)
                {
                    if ((DateTime.UtcNow - _lastWarningSentTime).TotalHours > 1)
                    {
                        string message = $"Main account {_settings.EthereumMainAccount} balance is less that {_settings.MainAccountMinBalance} ETH !";

                        await _logger.WriteWarningAsync("TransferContractPoolService", "InternalBalanceCheck", "", message);
                        await _slackNotifier.FinanceWarningAsync(message);

                        _lastWarningSentTime = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync("TransferContractPoolService", "InternalBalanceCheck", "", e);
            }
        }
    }
}
