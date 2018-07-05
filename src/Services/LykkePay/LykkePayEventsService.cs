using Autofac.Features.AttributeFilters;
using EthereumSamuraiApiCaller;
using EthereumSamuraiApiCaller.Models;
using Lykke.Job.EthereumCore.Contracts.Enums.LykkePay;
using Lykke.Job.EthereumCore.Contracts.Events.LykkePay;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Services;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using Lykke.Service.RabbitMQ;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Common;
using Lykke.Service.EthereumCore.Core.LykkePay;
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
