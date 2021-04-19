﻿using Lykke.Service.EthereumCore.BusinessModels;
using EthereumSamuraiApiCaller;
using EthereumSamuraiApiCaller.Models;
using Nethereum.Util;
using Lykke.Service.EthereumCore.Services.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Services.PrivateWallet
{
    public interface IEthereumIndexerService
    {
        Task<IEnumerable<ErcAddressHistoryModel>> GetTokenHistory(TokenTransaction addressTransactions);
        Task<IEnumerable<AddressHistoryModel>> GetAddressHistory(AddressTransaction addressTransactions);
        Task<TransactionContentModel> GetTransactionAsync(string transactionHash);
        Task<IEnumerable<InternalMessageModel>> GetInternalMessagesForTransactionAsync(string transactionHash);
        Task<IEnumerable<TransactionContentModel>> GetTransactionHistory(AddressTransaction addressTransactions);
        Task<IEnumerable<InternalMessageModel>> GetInternalMessagesHistory(AddressTransaction addressMessages);
        Task<BigInteger> GetEthBalance(string address);
    }

    public class EthereumIndexerService : IEthereumIndexerService
    {
        private AddressUtil _addressUtil;
        private IEthereumSamuraiAPI _ethereumSamuraiApi;

        public EthereumIndexerService(IEthereumSamuraiAPI ethereumSamuraiApi)
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
            var transactionResponse = transactionResponseRaw as TransactionFullInfoResponse;
            ThrowOnError(transactionResponseRaw);

            return new TransactionContentModel()
            {
                Transaction = MapTransactionResponseToModel(transactionResponse.Transaction),
                ErcTransfer = MapErcHistoryFromResponse(transactionResponse.Erc20Transfers ?? new List<Erc20TransferHistoryResponse>())
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

        public async Task<IEnumerable<ErcAddressHistoryModel>> GetTokenHistory(TokenTransaction addressTransactions)
        {
            List<string> tokenQuerySearch = null;
            if (!string.IsNullOrEmpty(addressTransactions.TokenAddress))
            {
                tokenQuerySearch = new List<string>()
                {
                    addressTransactions.TokenAddress
                };
            }
            var transactionResponseRaw = await _ethereumSamuraiApi.ApiErc20TransferHistoryGetErc20TransfersPostAsync(
                new GetErc20TransferHistoryRequest(addressTransactions.Address, null, tokenQuerySearch),
                addressTransactions.Start,
                addressTransactions.Count);
            List<ErcAddressHistoryModel> result = MapErcHistoryFromResponse(transactionResponseRaw);

            return result;
        }

        private List<ErcAddressHistoryModel> MapErcHistoryFromResponse(object transactionResponseRaw)
        {
            var transactionResponse = transactionResponseRaw as IList<Erc20TransferHistoryResponse>;
            ThrowOnError(transactionResponseRaw);
            int responseCount = transactionResponse?.Count ?? 0;
            List<ErcAddressHistoryModel> result = new List<ErcAddressHistoryModel>(responseCount);

            foreach (var transaction in transactionResponse)
            {
                result.Add(new ErcAddressHistoryModel()
                {
                    ContractAddress = transaction.Contract,
                    BlockNumber = (ulong)transaction.BlockNumber,
                    BlockTimestamp = (uint)transaction.BlockTimestamp,
                    BlockTimeUtc = DateUtils.UnixTimeStampToDateTimeUtc(transaction.BlockTimestamp),
                    From = transaction.FromProperty,
                    GasPrice = transaction.GasPrice,
                    GasUsed = transaction.GasUsed,
                    HasError = false,
                    MessageIndex = transaction.LogIndex,
                    To = transaction.To,
                    TransactionHash = transaction.TransactionHash,
                    Value = transaction.TransferAmount
                });
            }

            return result;
        }

        public async Task<IEnumerable<TransactionContentModel>> GetTransactionHistory(AddressTransaction addressTransactions)
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

        public async Task<IEnumerable<AddressHistoryModel>> GetAddressHistory(AddressTransaction addressTransactions)
        {
            var historyResponseRaw = await _ethereumSamuraiApi.ApiAddressHistoryByAddressGetAsync(addressTransactions.Address, addressTransactions.Count, addressTransactions.Start, null, null);
            var addressHistoryResponse = historyResponseRaw as FilteredAddressHistoryResponse;
            ThrowOnError(historyResponseRaw);
            int responseCount = addressHistoryResponse.History?.Count ?? 0;
            List<AddressHistoryModel> result = new List<AddressHistoryModel>(responseCount);

            foreach (var item in addressHistoryResponse.History)
            {
                result.Add(
                    new AddressHistoryModel()
                    {
                        MessageIndex = item.MessageIndex,
                        TransactionIndexInBlock = item.TransactionIndex,
                        BlockNumber = (ulong)item.BlockNumber,
                        BlockTimestamp = (uint)item.BlockTimestamp,
                        BlockTimeUtc = DateUtils.UnixTimeStampToDateTimeUtc(item.BlockTimestamp),
                        From = item.FromProperty,
                        HasError = item.HasError,
                        To = item.To,
                        TransactionHash = item.TransactionHash,
                        Value = item.Value,
                        GasPrice = item.GasPrice,
                        GasUsed = item.GasUsed
                    });
            }

            return result;
        }

        public async Task<IEnumerable<InternalMessageModel>> GetInternalMessagesHistory(AddressTransaction addressMessages)
        {
            var internalMessageResponseRaw = await _ethereumSamuraiApi.
                     ApiInternalMessagesByAddressGetAsync(addressMessages.Address, addressMessages.Count, addressMessages.Start, null, null);
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
                BlockNumber = (ulong)message.BlockNumber,
                Depth = message.Depth,
                FromAddress = message.FromAddress,
                MessageIndex = message.MessageIndex,
                ToAddress = message.ToAddress,
                TransactionHash = message.TransactionHash,
                Type = message.Type,
                Value = BigInteger.Parse(message.Value),
                BlockTimestamp = (uint)message.BlockTimeStamp,
                BlockTimeUtc = DateUtils.UnixTimeStampToDateTimeUtc(message.BlockTimeStamp)
            };
        }

        private static TransactionModel MapTransactionResponseToModel(TransactionResponse transaction)
        {
            if (transaction == null)
                return null;

            return new TransactionModel()
            {
                BlockHash = transaction.BlockHash,
                BlockNumber = (ulong)transaction.BlockNumber,
                BlockTimestamp = (uint)transaction.BlockTimestamp,
                ContractAddress = transaction.ContractAddress,
                From = transaction.FromProperty,
                Gas = transaction.Gas,
                GasPrice = transaction.GasPrice,
                GasUsed = transaction.GasUsed,
                Input = transaction.Input,
                Nonce = transaction.Nonce,
                To = transaction.To,
                TransactionHash = transaction.TransactionHash,
                TransactionIndex = transaction.TransactionIndex,
                Value = transaction.Value,
                BlockTimeUtc = DateUtils.UnixTimeStampToDateTimeUtc(transaction.BlockTimestamp),
                HasError = transaction.HasError
            };
        }
    }
}
