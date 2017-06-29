﻿using Services.Signature;
using System;
using System.Collections.Generic;
using System.Text;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using System.Threading.Tasks;
using EthereumSamuraiApiCaller;
using Microsoft.Rest;
using Newtonsoft.Json;
using System.Threading;
using EthereumSamuraiApiCaller.Models;
using System.Linq;
using System.Numerics;

namespace Service.UnitTests.Mocks
{
    public class MockEthereumSamuraiApi : IEthereumSamuraiApi
    {
        public Dictionary<string, string> AddressBalance = new Dictionary<string, string>()
        {
            { TestConstants.PW_ADDRESS, "0"}
        };

        public Dictionary<string, IEnumerable<TransactionResponse>> TransactionsDictionary = new Dictionary<string, IEnumerable<TransactionResponse>>()
        {
            { TestConstants.PW_ADDRESS, new List<TransactionResponse>()
            {
                new TransactionResponse()
                {
                    TransactionIndex = 0,
                    BlockTimestamp = 0,
                    BlockNumber = 1,
                    FromProperty = TestConstants.PW_ADDRESS,
                    Gas = "21000",
                    GasPrice = "30000000000",
                    Value = "3000000000000",
                    To = "0x0"
                },
                new TransactionResponse()
                {
                    TransactionIndex = 0,
                    BlockTimestamp = 10,
                    BlockNumber = 2,
                    FromProperty = TestConstants.PW_ADDRESS,
                    Gas = "21000",
                    GasPrice = "30000000000",
                    Value = "3000000000000",
                    To = "0x0"
                },
                new TransactionResponse()
                {
                    TransactionIndex = 0,
                    BlockTimestamp = 20,
                    BlockNumber = 3,
                    FromProperty = TestConstants.PW_ADDRESS,
                    Gas = "21000",
                    GasPrice = "30000000000",
                    Value = "3000000000000",
                    To = "0x0"
                }
            } }
        };

        public Uri BaseUri { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public JsonSerializerSettings SerializationSettings => throw new NotImplementedException();

        public JsonSerializerSettings DeserializationSettings => throw new NotImplementedException();

        public Task<HttpOperationResponse<object>> ApiBalanceGetBalanceByAddressGetWithHttpMessagesAsync(string address, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            string balance;
            AddressBalance.TryGetValue(address, out balance);
            if (balance == null)
            {
                balance = "0";
            }
            HttpOperationResponse<object> httpResponse = new HttpOperationResponse<object>()
            {
                Body = new BalanceResponse()
                {
                   Amount = balance
                }
            };

            return Task.FromResult(httpResponse);
        }

        public Task<HttpOperationResponse> ApiSystemIsAliveGetWithHttpMessagesAsync(Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<object>> ApiTransactionBlockHashByBlockHashGetWithHttpMessagesAsync(string blockHash, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<object>> ApiTransactionBlockNumberByBlockNumberGetWithHttpMessagesAsync(long blockNumber, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<object>> ApiTransactionByAddressGetWithHttpMessagesAsync(string address, int? start = default(int?), int? count = default(int?), Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            IEnumerable<TransactionResponse> addressTransactions;
            TransactionsDictionary.TryGetValue(address, out addressTransactions);
            if (addressTransactions == null)
            {
                addressTransactions = new List<TransactionResponse>();
            }
            HttpOperationResponse<object> httpResponse = new HttpOperationResponse<object>()
            {
                Body = new FilteredTransactionsResponse() { Transactions = addressTransactions
                .Skip(start.Value)
                .Take(count.Value).ToList() }
            };

            return Task.FromResult(httpResponse);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
