using EthereumSamuraiApiCaller;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Airlines;
using Lykke.Service.EthereumCore.Core.LykkePay;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Services;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using Lykke.Service.RabbitMQ;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Services.Airlines
{
    public class EventsService : ILykkePayEventsService
    {
        private readonly IAirlinesErc20DepositContractService _depositContractService;
        private readonly IEthereumIndexerService _ethereumIndexerService;

        public EventsService(
            IEthereumIndexerService ethereumIndexerService,
            IAirlinesErc20DepositContractService depositContractService)
        {
            _depositContractService = depositContractService;
            _ethereumIndexerService = ethereumIndexerService;
        }

        public async Task<(BigInteger? amount, string blockHash, ulong blockNumber)> IndexCashinEventsForErc20TransactionHashAsync(string transactionHash)
        {
            BigInteger result = 0;
            var transaction = await _ethereumIndexerService.GetTransactionAsync(transactionHash);

            if (transaction == null)
                return (null, null, 0);

            if (transaction.ErcTransfer != null)
            {
                //only one transfer could appear in deposit transaction
                foreach (var item in transaction.ErcTransfer)
                {
                    if (!await _depositContractService.ContainsAsync(item.From?.ToLower()))
                    { 
                        continue;
                    }

                    BigInteger.TryParse(item.Value, out result);
                }
            }

            return (result, transaction.Transaction?.BlockHash, transaction.Transaction?.BlockNumber ?? 0);
        }

        public async Task IndexCashinEventsForErc20Deposits()
        {
            throw new NotImplementedException();
        }
    }
}
