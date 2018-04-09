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
using System.Numerics;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Services.New
{
    public interface ILykkePayEventsService
    {
        Task IndexCashinEventsForErc20Deposits();
    }

    public class LykkePayEventsService : ILykkePayEventsService
    {
        private const string Erc20HotWalletMarker = "ERC20_HOTWALLET";

        private readonly IBaseSettings _baseSettings;
        private readonly IQueueFactory _queueFactory;
        private readonly IQueueExt _cashinQueue;
        private readonly IQueueExt _cointTransactionQueue;
        private readonly ICashinEventRepository _cashinEventRepository;
        private readonly IBlockSyncedRepository _blockSyncedRepository;
        private readonly AppSettings _settingsWrapper;
        private readonly IEthereumSamuraiApi _indexerApi;
        private readonly IErc20DepositContractService _depositContractService;

        public LykkePayEventsService(
            IBaseSettings baseSettings,
            ICashinEventRepository cashinEventRepository,
            IBlockSyncedRepository blockSyncedRepository,
            IQueueFactory queueFactory,
            AppSettings settingsWrapper,
            IEthereumSamuraiApi indexerApi,
            IErc20DepositContractService depositContractService)
        {
            _cashinEventRepository = cashinEventRepository;
            _blockSyncedRepository = blockSyncedRepository;
            _baseSettings = baseSettings;
            _queueFactory = queueFactory;
            _settingsWrapper = settingsWrapper;
            _indexerApi = indexerApi;
            _depositContractService = depositContractService;
            _cashinQueue = _queueFactory.Build(Constants.CashinCompletedEventsQueue);
            _cointTransactionQueue = _queueFactory.Build(Constants.HotWalletTransactionMonitoringQueue);
        }

        public async Task IndexCashinEventsForErc20Deposits()
        {
            var indexerStatusResponse = await _indexerApi.ApiSystemIsAliveGetWithHttpMessagesAsync();
            if (indexerStatusResponse.Response.IsSuccessStatusCode)
            {
                var responseContent = await indexerStatusResponse.Response.Content.ReadAsStringAsync();
                var indexerStatus = JObject.Parse(responseContent);
                var lastIndexedBlock = BigInteger.Parse(indexerStatus["blockchainTip"].Value<string>());
                var lastSyncedBlock = await GetLastSyncedBlockNumber(Erc20HotWalletMarker);

                while (++lastSyncedBlock <= lastIndexedBlock - _baseSettings.Level2TransactionConfirmation)
                {
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
                                if (!await _depositContractService.ContainsAsync(transfer.FromProperty))
                                {
                                    continue;
                                }

                                var coinTransactionMessage = new CoinTransactionMessage
                                {
                                    TransactionHash = transfer.TransactionHash
                                };

                                await _cashinEventRepository.InsertAsync(new CashinEvent
                                {
                                    CoinAdapterAddress = Erc20HotWalletMarker,
                                    Amount = transfer.TransferAmount,
                                    TransactionHash = transfer.TransactionHash,
                                    UserAddress = transfer.FromProperty,
                                    ContractAddress = transfer.Contract
                                });

                                await _cointTransactionQueue.PutRawMessageAsync(JsonConvert.SerializeObject(coinTransactionMessage));
                            }

                            break;
                        case ApiException exception:
                            throw new Exception($"Ethereum indexer responded with error: {exception.Error.Message}");
                        default:
                            throw new Exception($"Ethereum indexer returned unexpected response");
                    }

                    await _blockSyncedRepository.InsertAsync(new BlockSynced
                    {
                        BlockNumber = lastSyncedBlock.ToString(),
                        CoinAdapterAddress = Erc20HotWalletMarker
                    });
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

        private async Task IndexEventsInRange(string coinAdapterAddress, Nethereum.Contracts.Event coinCashInEvent, BigInteger from, BigInteger to)
        {
            var fromBlock = new Nethereum.RPC.Eth.DTOs.BlockParameter(new Nethereum.Hex.HexTypes.HexBigInteger(from));
            var toBlock = new Nethereum.RPC.Eth.DTOs.BlockParameter(new Nethereum.Hex.HexTypes.HexBigInteger(to));
            var filter = await coinCashInEvent.CreateFilterBlockRangeAsync(fromBlock, toBlock);
            var filterByCaller = await coinCashInEvent.GetAllChanges<CoinCashinEvent>(filter);

            filterByCaller.ForEach(async @event =>
            {
                string transactionHash = @event.Log.TransactionHash;
                CoinEventCashinCompletedMessage cashinTransactionMessage = new CoinEventCashinCompletedMessage()
                {
                    TransactionHash = transactionHash
                };

                await _cashinEventRepository.InsertAsync(new CashinEvent()
                {
                    CoinAdapterAddress = coinAdapterAddress,
                    Amount = @event.Event.Amount.ToString(),
                    TransactionHash = transactionHash,
                    UserAddress = @event.Event.Caller
                });

                await _cashinQueue.PutRawMessageAsync(Newtonsoft.Json.JsonConvert.SerializeObject(cashinTransactionMessage));
            });

            await _blockSyncedRepository.InsertAsync(new BlockSynced() { BlockNumber = to.ToString(), CoinAdapterAddress = coinAdapterAddress });
        }
    }
}
