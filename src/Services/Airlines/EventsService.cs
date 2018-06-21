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
using Autofac.Features.AttributeFilters;
using Lykke.Service.EthereumCore.Core.Common;
using Lykke.Service.EthereumCore.Services.Common;

namespace Lykke.Service.EthereumCore.Services.Airlines
{
    public class AirlinesEventsService : EventsServiceCommon
    {
        public AirlinesEventsService(
            IBlockSyncedByHashRepository blockSyncedRepository,
            IEthereumSamuraiAPI indexerApi,
            IEthereumIndexerService ethereumIndexerService,
            IRabbitQueuePublisher rabbitQueuePublisher,
            IAggregatedErc20DepositContractLocatorService depositContractService) : 
            base(blockSyncedRepository,
                indexerApi,
                ethereumIndexerService,
                rabbitQueuePublisher,
                depositContractService)
        {
        }

        protected override string HotWalletMarker
        {
            get
            {
                return "Airlines_ERC20_HOTWALLET";
            }
        }
    }
}
