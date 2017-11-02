using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EthereumSamuraiApiCaller;
using EthereumSamuraiApiCaller.Models;
using Microsoft.Rest;
using Newtonsoft.Json;

namespace Service.UnitTests.Mocks
{
    public class MockEthereumSamuraiApi : IEthereumSamuraiApi
    {
        public Dictionary<string, string> AddressBalance = new Dictionary<string, string>
        {
            {
                TestConstants.PW_ADDRESS, "0"
            }
        };

        public Dictionary<string, Dictionary<string, string>> Erc20Balances =
            new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "0x5711cbe8d5577699c371760337b5ea33963c8c8f",
                    new Dictionary<string, string>
                    {
                        {
                            "0x95a48dca999c89e4e284930d9b9af973a7481287",
                            "800000000"
                        },
                        {
                            "0x1c6e03b7b2c9a8cc27e618a61a9e9d64479954c1",
                            "100"
                        }
                    }
                }
            };

        public Dictionary<string, IEnumerable<InternalMessageResponse>> MessageDictionary =
            new Dictionary<string, IEnumerable<InternalMessageResponse>>
            {
                {
                    TestConstants.PW_ADDRESS, new List<InternalMessageResponse>
                    {
                        new InternalMessageResponse
                        {
                            TransactionHash = "0x10",
                            BlockNumber     = 1,
                            Depth           = 1,
                            FromAddress     = TestConstants.PW_ADDRESS,
                            MessageIndex    = 0,
                            ToAddress       = TestConstants.PW_ADDRESS,
                            Type            = "TRANSFER",
                            Value           = "10000000000",
                            BlockTimeStamp  = 0
                        },
                        new InternalMessageResponse
                        {
                            TransactionHash = "0x20",
                            BlockNumber     = 1,
                            Depth           = 1,
                            FromAddress     = TestConstants.PW_ADDRESS,
                            MessageIndex    = 0,
                            ToAddress       = TestConstants.PW_ADDRESS,
                            Type            = "TRANSFER",
                            Value           = "10000000000",
                            BlockTimeStamp  = 0
                        },
                        new InternalMessageResponse
                        {
                            TransactionHash = "0x30",
                            BlockNumber     = 1,
                            Depth           = 1,
                            FromAddress     = TestConstants.PW_ADDRESS,
                            MessageIndex    = 1,
                            ToAddress       = TestConstants.PW_ADDRESS,
                            Type            = "TRANSFER",
                            Value           = "10000000000",
                            BlockTimeStamp  = 0
                        }
                    }
                }
            };

        public Dictionary<string, IEnumerable<TransactionResponse>> TransactionsDictionary =
            new Dictionary<string, IEnumerable<TransactionResponse>>
            {
                {
                    TestConstants.PW_ADDRESS, new List<TransactionResponse>
                    {
                        new TransactionResponse
                        {
                            TransactionHash  = "0x10",
                            TransactionIndex = 0,
                            BlockTimestamp   = 0,
                            BlockNumber      = 1,
                            FromProperty     = TestConstants.PW_ADDRESS,
                            Gas              = "21000",
                            GasPrice         = "30000000000",
                            Value            = "3000000000000",
                            To               = "0x0",
                            HasError         = false
                        },
                        new TransactionResponse
                        {
                            TransactionHash  = "0x20",
                            TransactionIndex = 0,
                            BlockTimestamp   = 10,
                            BlockNumber      = 2,
                            FromProperty     = TestConstants.PW_ADDRESS,
                            Gas              = "21000",
                            GasPrice         = "30000000000",
                            Value            = "3000000000000",
                            To               = "0x0",
                            HasError         = false
                        },
                        new TransactionResponse
                        {
                            TransactionHash  = "0x30",
                            TransactionIndex = 0,
                            BlockTimestamp   = 20,
                            BlockNumber      = 3,
                            FromProperty     = TestConstants.PW_ADDRESS,
                            Gas              = "21000",
                            GasPrice         = "30000000000",
                            Value            = "3000000000000",
                            To               = "0x0",
                            HasError         = false
                        }
                    }
                }
            };


        public Uri BaseUri
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public JsonSerializerSettings DeserializationSettings 
            => throw new NotImplementedException();

        public JsonSerializerSettings SerializationSettings 
            => throw new NotImplementedException();

        public Task<HttpOperationResponse<object>> ApiAddressHistoryByAddressGetWithHttpMessagesAsync(
            string address,
            long? startBlock                               = default(long?),
            long? stopBlock                                = default(long?),
            int? start                                     = default(int?),
            int? count                                     = default(int?),
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken            = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<object>> ApiBalanceGetBalanceByAddressGetWithHttpMessagesAsync(
            string address,
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken            = default(CancellationToken))
        {
            AddressBalance.TryGetValue(address, out string balance);

            if (balance == null)
            {
                balance = "0";
            }
                
            var httpResponse = new HttpOperationResponse<object>
            {
                Body = new BalanceResponse
                {
                    Amount = balance
                }
            };

            return Task.FromResult(httpResponse);
        }

        public Task<HttpOperationResponse<object>> ApiErc20BalanceGetErc20BalanceByAddressPostWithHttpMessagesAsync(
            string address,
            IList<string> contracts                        = null,
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken            = new CancellationToken())
        {
            var httpResponse = new HttpOperationResponse<object>
            {
                Body = Erc20Balances.Where(x => x.Key == address)
                    .SelectMany(x => x.Value)
                    .Where(x => contracts.Contains(x.Key))
                    .Select(x => new Erc20BalanceResponse
                    {
                        Address  = address,
                        Amount   = x.Value,
                        Contract = x.Key
                    })
                    .ToList()
            };

            return Task.FromResult(httpResponse);
        }

        public Task<HttpOperationResponse<object>> ApiErc20BalanceGetErc20BalancePostWithHttpMessagesAsync(GetErc20BalanceRequest request = null, int? start = null, int? count = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<object>> ApiErc20TokenByContractAddressGetWithHttpMessagesAsync(string contractAddress, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<object>> ApiErc20TokenGetWithHttpMessagesAsync(string query = null, int? count = null, int? start = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<object>> ApiErc20TransferHistoryGetErc20TransfersPostWithHttpMessagesAsync(GetErc20TransferHistoryRequest request = null, int? start = null, int? count = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<object>> ApiInternalMessagesByAddressGetWithHttpMessagesAsync(
            string address,
            long? startBlock,
            long? stopBlock, int? start                    = default(int?),
            int? count                                     = default(int?),
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken            = default(CancellationToken))
        {
            MessageDictionary.TryGetValue(address, out IEnumerable<InternalMessageResponse> addressMessages);
            if (addressMessages == null)
            {
                addressMessages = new List<InternalMessageResponse>();
            }
            var httpResponse = new HttpOperationResponse<object>
            {
                Body = new FilteredInternalMessageResponse
                {
                    Messages = addressMessages
                        .Skip(start.Value)
                        .Take(count.Value)
                        .ToList()
                }
            };

            return Task.FromResult(httpResponse);
        }

        public Task<HttpOperationResponse<object>> ApiInternalMessagesTxHashByTransactionHashGetWithHttpMessagesAsync(
            string transactionHash,
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken            = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse> ApiSystemIsAliveGetWithHttpMessagesAsync(
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken            = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<object>> ApiTransactionBlockHashByBlockHashGetWithHttpMessagesAsync(
            string blockHash,
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken            = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<object>> ApiTransactionBlockNumberByBlockNumberGetWithHttpMessagesAsync(
            long blockNumber,
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken            = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<object>> ApiTransactionByAddressGetWithHttpMessagesAsync(
            string address,
            int? start                                     = default(int?),
            int? count                                     = default(int?),
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken            = default(CancellationToken))
        {
            TransactionsDictionary.TryGetValue(address, out IEnumerable<TransactionResponse> addressTransactions);

            if (addressTransactions == null)
            {
                addressTransactions = new List<TransactionResponse>();
            }
                
            var httpResponse = new HttpOperationResponse<object>
            {
                Body = new FilteredTransactionsResponse
                {
                    Transactions = addressTransactions
                        .Skip(start.Value)
                        .Take(count.Value)
                        .ToList()
                }
            };

            return Task.FromResult(httpResponse);
        }

        public Task<HttpOperationResponse<object>> ApiTransactionTxHashByTransactionHashGetWithHttpMessagesAsync(
            string transactionHash,
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken            = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}