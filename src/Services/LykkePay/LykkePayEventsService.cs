using AzureStorage.Queue;
using EthereumSamuraiApiCaller;
using EthereumSamuraiApiCaller.Models;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.Service.EthereumCore.Services.Coins.Models;
using Lykke.Service.EthereumCore.Services.New.Models;
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using Nethereum.Web3;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using Lykke.Job.EthereumCore.Contracts.Enums.LykkePay;
using Lykke.Job.EthereumCore.Contracts.Events.LykkePay;
using Lykke.Service.EthereumCore.Core.Services;
using Lykke.Service.RabbitMQ;

namespace Lykke.Service.EthereumCore.Services.New
{
    public interface ILykkePayEventsService
    {
        Task IndexCashinEventsForErc20Deposits();
        Task<BigInteger?> IndexCashinEventsForErc20TransactionHashAsync(string transactionHash);
    }

    public class LykkePayEventsService : ILykkePayEventsService
    {
        private const string Erc20HotWalletMarker = "LykkePay_ERC20_HOTWALLET";

        private readonly IBaseSettings _baseSettings;
        private readonly IBlockSyncedByHashRepository _blockSyncedRepository;
        private readonly AppSettings _settingsWrapper;
        private readonly IEthereumSamuraiAPI _indexerApi;
        private readonly IErc20DepositContractService _depositContractService;
        private IEthereumIndexerService _ethereumIndexerService;
        private IWeb3 _web3;
        private IRabbitQueuePublisher _rabbitQueuePublisher;

        public LykkePayEventsService(
            IBaseSettings baseSettings,
            ICashinEventRepository cashinEventRepository,
            IBlockSyncedByHashRepository blockSyncedRepository,
            IQueueFactory queueFactory,
            AppSettings settingsWrapper,
            IEthereumSamuraiAPI indexerApi,
            IEthereumIndexerService ethereumIndexerService,
            IWeb3 web3,
            IRabbitQueuePublisher rabbitQueuePublisher,
            [KeyFilter(Constants.LykkePayKey)]IErc20DepositContractService depositContractService)
        {
            _blockSyncedRepository = blockSyncedRepository;
            _baseSettings = baseSettings;
            _settingsWrapper = settingsWrapper;
            _indexerApi = indexerApi;
            _depositContractService = depositContractService;
            _ethereumIndexerService = ethereumIndexerService;
            _rabbitQueuePublisher = rabbitQueuePublisher;
            _web3 = web3;
        }

        public async Task<BigInteger?> IndexCashinEventsForErc20TransactionHashAsync(string transactionHash)
        {
            BigInteger result = 0;
            var transaction = await _ethereumIndexerService.GetTransactionAsync(transactionHash);

            if (transaction == null)
                return null;

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

            return result;
        }

        public async Task IndexCashinEventsForErc20Deposits()
        {
            var indexerStatusResponse = await _indexerApi.ApiIsAliveGetWithHttpMessagesAsync();
            if (indexerStatusResponse.Response.IsSuccessStatusCode)
            {
                var responseContent = await indexerStatusResponse.Response.Content.ReadAsStringAsync();
                var indexerStatus = JObject.Parse(responseContent);
                var lastIndexedBlock = BigInteger.Parse(indexerStatus["blockchainTip"].Value<string>());
                var lastSyncedBlock = await GetLastSyncedBlockNumber(Erc20HotWalletMarker);

                while (lastSyncedBlock <= lastIndexedBlock)
                {
                    //_web3.Eth.Blocks.
                    var transfersResponse = await _indexerApi.ApiErc20TransferHistoryGetErc20TransfersPostAsync
                    (
                        new GetErc20TransferHistoryRequest
                        {
                            AssetHolder = _settingsWrapper.Ethereum.HotwalletAddress?.ToLower(),
                            BlockNumber = (long)lastSyncedBlock,
                        }
                    );

                    switch (transfersResponse)
                    {
                        case IEnumerable<Erc20TransferHistoryResponse> transfers:
                            foreach (var transfer in transfers)
                            {
                                // Ignore transfers from not deposit contract addresses
                                if (!await _depositContractService.ContainsAsync(transfer.To))
                                {
                                    continue;
                                }

                                var id = $"Detected_{Guid.NewGuid()}";
                                await _rabbitQueuePublisher.PublshEvent(new TransferEvent(id,
                                    transfer.TransactionHash,
                                    transfer.TransferAmount,
                                    transfer.Contract,
                                    transfer.FromProperty,
                                    transfer.To,
                                    SenderType.Customer,
                                    EventType.Detected
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

                    var parentBlock = await _blockSyncedRepository.GetByPartitionAndHashAsync(Erc20HotWalletMarker, blockCurrent.ParentHash);
                    if (parentBlock == null)
                    {
                        lastSyncedBlock--;
                        await _blockSyncedRepository.DeleteByPartitionAndHashAsync(Erc20HotWalletMarker, blockCurrent.ParentHash);
                        continue;
                    }

                    await _blockSyncedRepository.InsertAsync(new BlockSyncedByHash()
                    {
                        BlockNumber = lastSyncedBlock.ToString(),
                        Partition = Erc20HotWalletMarker,
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
