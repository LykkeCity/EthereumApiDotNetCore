using Autofac.Features.AttributeFilters;
using EthereumSamuraiApiCaller;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using Lykke.Service.RabbitMQ;
using Lykke.Service.EthereumCore.Core.Common;
using Lykke.Service.EthereumCore.Services.Common;

namespace Lykke.Service.EthereumCore.Services.New
{
    public class LykkePayEventsService : EventsServiceCommon
    {
        protected override string HotWalletMarker
        {
            get
            {
                return "LykkePay_ERC20_HOTWALLET";
            }
        }

        public LykkePayEventsService(
            IBlockSyncedByHashRepository blockSyncedRepository,
            IEthereumSamuraiAPI indexerApi,
            IEthereumIndexerService ethereumIndexerService,
            IRabbitQueuePublisher rabbitQueuePublisher,
            [KeyFilter(Constants.DefaultKey)] IAggregatedErc20DepositContractLocatorService erc20DepositContractLocatorService,
            [KeyFilter(Constants.AirLinesKey)]IHotWalletTransactionRepository airHotWalletCashoutTransactionRepository,
            [KeyFilter(Constants.LykkePayKey)]IHotWalletTransactionRepository lpHotWalletCashoutTransactionRepository) :
            base(blockSyncedRepository,
                indexerApi,
                ethereumIndexerService,
                rabbitQueuePublisher,
                erc20DepositContractLocatorService,
                airHotWalletCashoutTransactionRepository,
                lpHotWalletCashoutTransactionRepository)
        {

        }
    }
}
