using BusinessModels;
using Core.Exceptions;
using EthereumSamuraiApiCaller;
using EthereumSamuraiApiCaller.Models;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Util;
using Nethereum.Web3;
using Services.Signature;
using Services.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Services.PrivateWallet
{
    public interface IEthereumIndexerService
    {
        Task<IEnumerable<AddressHistoryModel>> GetAddressHistory(AddressTransactions addressTransactions);
        Task<TransactionContentModel> GetTransactionAsync(string transactionHash);
        Task<IEnumerable<InternalMessageModel>> GetInternalMessagesForTransactionAsync(string transactionHash);
        Task<IEnumerable<TransactionContentModel>> GetTransactionHistory(AddressTransactions addressTransactions);
        Task<IEnumerable<InternalMessageModel>> GetInternalMessagesHistory(AddressTransactions addressMessages);
        Task<BigInteger> GetEthBalance(string address);
    }

    public class EthereumIndexerService : IEthereumIndexerService
    {
        private AddressUtil _addressUtil;
        private IEthereumSamuraiApi _ethereumSamuraiApi;

        public EthereumIndexerService(IEthereumSamuraiApi ethereumSamuraiApi)
        {
            _addressUtil = new AddressUtil();
            _ethereumSamuraiApi = ethereumSamuraiApi;
        }

        public async Task<BigInteger> GetEthBalance(string address)
        {
            var response = await _ethereumSamuraiApi.ApiBalanceGetBalanceByAddressGetAsync(address);
            var balanceResponse = response as BalanceResponse;
            ThrowOnError(response);
            BigInteger balance = BigInteger.Parse(balanceResponse.Amount);

            return balance;
        }

        public async Task<TransactionContentModel> GetTransactionAsync(string transactionHash)
        {
            var transactionResponseRaw = await _ethereumSamuraiApi.ApiTransactionTxHashByTransactionHashGetAsync(transactionHash);
            var transactionResponse = transactionResponseRaw as TransactionResponse;
            ThrowOnError(transactionResponseRaw);

            return new TransactionContentModel()
            {
                Transaction = MapTransactionResponseToModel(transactionResponse)
            };
        }

        public async Task<IEnumerable<InternalMessageModel>> GetInternalMessagesForTransactionAsync(string transactionHash)
        {
            var internalMessageResponseRaw = await _ethereumSamuraiApi.
                     ApiInternalMessagesTxHashByTransactionHashGetAsync(transactionHash);
            FilteredInternalMessageResponse internalMessageResponse = internalMessageResponseRaw as FilteredInternalMessageResponse;
            ThrowOnError(internalMessageResponseRaw);
            int responseCount = internalMessageResponse.Messages?.Count ?? 0;
            List<InternalMessageModel> result = new List<InternalMessageModel>(responseCount);

            foreach (var message in internalMessageResponse.Messages)
            {
                result.Add(MapInternalMessageResponseToModel(message));
            }

            return result;
        }

        public async Task<IEnumerable<TransactionContentModel>> GetTransactionHistory(AddressTransactions addressTransactions)
        {
            var transactionResponseRaw = await _ethereumSamuraiApi.ApiTransactionByAddressGetAsync(addressTransactions.Address, addressTransactions.Start, addressTransactions.Count);
            var transactionResponse = transactionResponseRaw as FilteredTransactionsResponse;
            ThrowOnError(transactionResponseRaw);
            int responseCount = transactionResponse.Transactions?.Count ?? 0;
            List<TransactionContentModel> result = new List<TransactionContentModel>(responseCount);

            foreach (var transaction in transactionResponse.Transactions)
            {
                result.Add(new TransactionContentModel()
                {
                    Transaction = MapTransactionResponseToModel(transaction)
                });
            }

            return result;
        }

        public async Task<IEnumerable<AddressHistoryModel>> GetAddressHistory(AddressTransactions addressTransactions)
        {
            var historyResponseRaw = await _ethereumSamuraiApi.ApiAddressHistoryByAddressGetAsync(addressTransactions.Address, null, null, addressTransactions.Start, addressTransactions.Count);
            var addressHistoryResponse = historyResponseRaw as FilteredAddressHistoryResponse;
            ThrowOnError(historyResponseRaw);
            int responseCount = addressHistoryResponse.History?.Count ?? 0;
            List<AddressHistoryModel> result = new List<AddressHistoryModel>(responseCount);

            foreach (var item in addressHistoryResponse.History)
            {
                result.Add(
                    new AddressHistoryModel()
                    {
                        MessageIndex = item.MessageIndex.Value,
                        TransactionIndexInBlock = item.TransactionIndex.Value,
                        BlockNumber = (ulong)item.BlockNumber.Value,
                        BlockTimestamp = (uint)item.BlockTimestamp.Value,
                        BlockTimeUtc = DateUtils.UnixTimeStampToDateTimeUtc(item.BlockTimestamp.Value),
                        From = item.FromProperty,
                        HasError = item.HasError.Value,
                        To = item.To,
                        TransactionHash = item.TransactionHash,
                        Value = item.Value,
                    });
            }

            return result;
        }

        public async Task<IEnumerable<InternalMessageModel>> GetInternalMessagesHistory(AddressTransactions addressMessages)
        {
            var internalMessageResponseRaw = await _ethereumSamuraiApi.
                     ApiInternalMessagesByAddressGetAsync(addressMessages.Address, null, null, addressMessages.Start, addressMessages.Count);
            FilteredInternalMessageResponse internalMessageResponse = internalMessageResponseRaw as FilteredInternalMessageResponse;
            ThrowOnError(internalMessageResponseRaw);
            int responseCount = internalMessageResponse.Messages?.Count ?? 0;
            List<InternalMessageModel> result = new List<InternalMessageModel>(responseCount);

            foreach (var message in internalMessageResponse.Messages)
            {
                result.Add(MapInternalMessageResponseToModel(message));
            }

            return result;
        }

        private void ThrowOnError(object transactionResponse)
        {
            if (transactionResponse == null)
            {
                var exception = transactionResponse as ApiException;
                var errorMessage = exception?.Error?.Message ?? "Response is empty";

                throw new Exception(errorMessage);
            }
        }

        private static InternalMessageModel MapInternalMessageResponseToModel(InternalMessageResponse message)
        {
            return new InternalMessageModel()
            {
                BlockNumber = (ulong)message.BlockNumber.Value,
                Depth = message.Depth.Value,
                FromAddress = message.FromAddress,
                MessageIndex = message.MessageIndex.Value,
                ToAddress = message.ToAddress,
                TransactionHash = message.TransactionHash,
                Type = message.Type,
                Value = BigInteger.Parse(message.Value),
                BlockTimestamp = (uint)message.BlockTimeStamp,
                BlockTimeUtc = DateUtils.UnixTimeStampToDateTimeUtc(message.BlockTimeStamp.Value)
            };
        }

        private static TransactionModel MapTransactionResponseToModel(TransactionResponse transaction)
        {
            return new TransactionModel()
            {
                BlockHash = transaction.BlockHash,
                BlockNumber = (ulong)transaction.BlockNumber.Value,
                BlockTimestamp = (uint)transaction.BlockTimestamp.Value,
                ContractAddress = transaction.ContractAddress,
                From = transaction.FromProperty,
                Gas = transaction.Gas,
                GasPrice = transaction.GasPrice,
                GasUsed = transaction.GasUsed,
                Input = transaction.Input,
                Nonce = transaction.Nonce,
                To = transaction.To,
                TransactionHash = transaction.TransactionHash,
                TransactionIndex = transaction.TransactionIndex.Value,
                Value = transaction.Value,
                BlockTimeUtc = DateUtils.UnixTimeStampToDateTimeUtc(transaction.BlockTimestamp.Value),
                HasError = transaction.HasError.Value
            };
        }
    }
}
