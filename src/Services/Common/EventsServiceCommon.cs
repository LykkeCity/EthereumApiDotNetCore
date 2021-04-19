using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using EthereumSamuraiApiCaller;
using EthereumSamuraiApiCaller.Models;
using Lykke.Job.EthereumCore.Contracts.Enums.LykkePay;
using Lykke.Job.EthereumCore.Contracts.Events.LykkePay;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Common;
using Lykke.Service.EthereumCore.Core.LykkePay;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using Lykke.Service.RabbitMQ;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.EthereumCore.Services.Common
{
    public abstract class EventsServiceCommon : ILykkePayEventsService
    {
        protected abstract string HotWalletMarker { get; }

        private readonly IBlockSyncedByHashRepository _blockSyncedRepository;
        private readonly IEthereumSamuraiAPI _indexerApi;
        private readonly IAggregatedErc20DepositContractLocatorService _depositContractService;
        private readonly IEthereumIndexerService _ethereumIndexerService;
        private readonly IRabbitQueuePublisher _rabbitQueuePublisher;
        private readonly IHotWalletTransactionRepository _airHotWalletCashoutTransactionRepository;
        private readonly IHotWalletTransactionRepository _lpHotWalletCashoutTransactionRepository;

        public EventsServiceCommon(
            IBlockSyncedByHashRepository blockSyncedRepository,
            IEthereumSamuraiAPI indexerApi,
            IEthereumIndexerService ethereumIndexerService,
            IRabbitQueuePublisher rabbitQueuePublisher,
            IAggregatedErc20DepositContractLocatorService depositContractService,
            [KeyFilter(Constants.AirLinesKey)]IHotWalletTransactionRepository airHotWalletCashoutTransactionRepository,
            [KeyFilter(Constants.LykkePayKey)]IHotWalletTransactionRepository lpHotWalletCashoutTransactionRepository)
        {
            _blockSyncedRepository = blockSyncedRepository;
            _indexerApi = indexerApi;
            _depositContractService = depositContractService;
            _ethereumIndexerService = ethereumIndexerService;
            _rabbitQueuePublisher = rabbitQueuePublisher;
            _airHotWalletCashoutTransactionRepository = airHotWalletCashoutTransactionRepository;
            _lpHotWalletCashoutTransactionRepository = lpHotWalletCashoutTransactionRepository;
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
                    if (!(await _depositContractService.ContainsWithTypeAsync(item.From?.ToLower())).Item1)
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
            var indexerStatusResponse = await _indexerApi.ApiIsAliveGetWithHttpMessagesAsync();
            if (indexerStatusResponse.Response.IsSuccessStatusCode)
            {
                var responseContent = await indexerStatusResponse.Response.Content.ReadAsStringAsync();
                var indexerStatus = JObject.Parse(responseContent);
                var lastIndexedBlock = BigInteger.Parse(indexerStatus["blockchainTip"].Value<string>());
                var lastSyncedBlock = await GetLastSyncedBlockNumber(HotWalletMarker);

                while (lastSyncedBlock <= lastIndexedBlock)
                {
                    //Get all transfers from block
                    var transfersResponse = await _indexerApi.ApiErc20TransferHistoryGetErc20TransfersPostAsync
                    (
                        new GetErc20TransferHistoryRequest
                        {
                            BlockNumber = (long)lastSyncedBlock,
                        }
                    );

                    switch (transfersResponse)
                    {
                        case IEnumerable<Erc20TransferHistoryResponse> transfers:
                            foreach (var transfer in transfers)
                            {
                                // Ignore transfers from not deposit contract addresses
                                var checkResult = await _depositContractService.ContainsWithTypeAsync(transfer.To);
                                if (!checkResult.Item1)
                                {
                                    continue;
                                }

                                string trHash = transfer.TransactionHash ?? "";

                                string id =
                                    (await _airHotWalletCashoutTransactionRepository.GetByTransactionHashAsync(trHash))
                                    ?.OperationId ??
                                    (await _lpHotWalletCashoutTransactionRepository.GetByTransactionHashAsync(trHash))
                                    ?.OperationId ?? null;
                                
                                await _rabbitQueuePublisher.PublshEvent(new TransferEvent(id,
                                    transfer.TransactionHash,
                                    transfer.TransferAmount,
                                    transfer.Contract,
                                    transfer.FromProperty,
                                    transfer.To,
                                    transfer.BlockHash,
                                    (ulong)transfer.BlockNumber,
                                    SenderType.Customer,
                                    EventType.Detected,
                                    (Job.EthereumCore.Contracts.Enums.LykkePay.WorkflowType)checkResult.Item2,
                                    DateTime.UtcNow
                                    ));
                            }

                            break;
                        case ApiException exception:
                            throw new Exception($"Ethereum indexer responded with error: {exception.Error.Message}");
                        default:
                            throw new Exception($"Ethereum indexer returned unexpected response");
                    }

                    var blockCurrent = (await _indexerApi.ApiBlockNumberByBlockNumberGetAsync((long)lastSyncedBlock)) as BlockResponse;

                    if (blockCurrent == null)
                        return;

                    var parentBlock = await _blockSyncedRepository.GetByPartitionAndHashAsync(HotWalletMarker, blockCurrent.ParentHash);
                    if (parentBlock == null)
                    {
                        lastSyncedBlock--;
                        await _blockSyncedRepository.DeleteByPartitionAndHashAsync(HotWalletMarker, blockCurrent.ParentHash);
                        continue;
                    }

                    await _blockSyncedRepository.InsertAsync(new BlockSyncedByHash()
                    {
                        BlockNumber = lastSyncedBlock.ToString(),
                        Partition = HotWalletMarker,
                        BlockHash = blockCurrent.BlockHash
                    });

                    lastSyncedBlock++;
                }
            }
            else
            {
                throw new Exception("Can not obtain ethereum indexer status.");
            }
        }

        private async Task<BigInteger> GetLastSyncedBlockNumber(string coinAdapterAddress)
        {
            string lastSyncedBlockNumber = (await _blockSyncedRepository.GetLastSyncedAsync(coinAdapterAddress))?.BlockNumber;
            BigInteger lastSynced;
            BigInteger.TryParse(lastSyncedBlockNumber, out lastSynced);

            return lastSynced;
        }
    }
}
