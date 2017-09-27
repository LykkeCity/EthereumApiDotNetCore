using Core;
using Core.Repositories;
using Core.Settings;
using Nethereum.Web3;
using Services.New.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using AzureStorage.Queue;

namespace Services.New
{
    public interface ITransactionEventsService
    {
        Task IndexCashinEvents(string coinAdapterAddress, string deployedTransactionHash);
        Task MonitorNewEvents(string coinAdapterAddress);
        Task<ICashinEvent> GetCashinEvent(string transactionHash);
    }

    public class TransactionEventsService : ITransactionEventsService
    {
        private readonly Web3 _web3;
        private readonly IBaseSettings _baseSettings;
        private readonly IQueueFactory _queueFactory;
        private readonly IQueueExt _cashinQueue;
        private readonly ICoinRepository _coinRepository;
        private readonly ICashinEventRepository _cashinEventRepository;
        private readonly IBlockSyncedRepository _blockSyncedRepository;

        public TransactionEventsService(Web3 web3,
            IBaseSettings baseSettings,
            ICoinRepository coinRepository,
            ICashinEventRepository cashinEventRepository,
            IBlockSyncedRepository blockSyncedRepository,
            IQueueFactory queueFactory)
        {
            _cashinEventRepository = cashinEventRepository;
            _coinRepository = coinRepository;
            _web3 = web3;
            _blockSyncedRepository = blockSyncedRepository;
            _baseSettings = baseSettings;
            _queueFactory = queueFactory;
            _cashinQueue = _queueFactory.Build(Constants.CashinCompletedEventsQueue);
        }

        public async Task IndexCashinEvents(string coinAdapterAddress, string deployedTransactionHash)
        {
            var lastBlock = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var contract = _web3.Eth.GetContract(_baseSettings.CoinAbi, coinAdapterAddress);
            var coinCashInEvent = contract.GetEvent("CoinCashIn");
            BigInteger lastSynced = await GetLastSyncedBlockNumber(coinAdapterAddress);
            var tranaction = await _web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(deployedTransactionHash);
            BigInteger contractDeployBlockNumber = tranaction.BlockNumber;
            BigInteger indexStartBlock = lastSynced > contractDeployBlockNumber ? lastSynced : contractDeployBlockNumber;
            int scanRange = 1000;

            for (BigInteger from = indexStartBlock; from < lastBlock; from += scanRange + 1)
            {
                BigInteger to = from + scanRange;
                to = to < lastBlock ? to : lastBlock;
                await IndexEventsInRange(coinAdapterAddress, coinCashInEvent, from, to);
            }
        }

        public async Task MonitorNewEvents(string coinAdapterAddress)
        {
            var contract = _web3.Eth.GetContract(_baseSettings.CoinAbi, coinAdapterAddress);
            var coinCashInEvent = contract.GetEvent("CoinCashIn");
            var lastBlock = await GetLastSyncedBlockNumber(coinAdapterAddress);
            var lastRpcBlock = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();

            if (lastRpcBlock.Value == lastBlock)
            {
                return;
            }

            await IndexEventsInRange(coinAdapterAddress, coinCashInEvent, lastBlock, lastRpcBlock.Value);
        }

        public async Task<ICashinEvent> GetCashinEvent(string transactionHash)
        {
            ICashinEvent @event = await _cashinEventRepository.GetAsync(transactionHash);

            return @event;
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
