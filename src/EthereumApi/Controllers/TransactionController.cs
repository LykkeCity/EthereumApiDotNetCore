using BusinessModels;
using Core.Exceptions;
using EthereumApi.Models;
using EthereumApi.Models.Indexer;
using EthereumApi.Models.Models;
using EthereumApi.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Services.PrivateWallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EthereumApi.Controllers
{
    [Route("api/transactions")]
    [Produces("application/json")]
    public class TransactionController : Controller
    {
        private IEthereumIndexerService _ethereumIndexerService;

        public TransactionController(IEthereumIndexerService ethereumIndexerService)
        {
            _ethereumIndexerService = ethereumIndexerService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(FilteredTransactionsResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetTransaction([FromBody]EthereumApi.Models.Models.AddressTransactions addressTransactions)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            BusinessModels.AddressTransaction request = new BusinessModels.AddressTransaction()
            {
                Address = addressTransactions.Address,
                Count = addressTransactions.Count,
                Start = addressTransactions.Start,
            };

            IEnumerable<TransactionContentModel> transactions = await _ethereumIndexerService.GetTransactionHistory(request);
            IEnumerable<Models.Indexer.TransactionResponse> result = transactions.Select(transactionContent =>
            {
                return MapTransactionModelContentToResponse(transactionContent);
            });

            return Ok(new FilteredTransactionsResponse()
            {
                Transactions = result
            });
        }

        [HttpPost("txHash/{transactionHash}")]
        [ProducesResponseType(typeof(Models.Indexer.TransactionResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetAddressHistory([FromRoute] string transactionHash)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            TransactionContentModel transaction = await _ethereumIndexerService.GetTransactionAsync(transactionHash);
            Models.Indexer.TransactionResponse result = MapTransactionModelContentToResponse(transaction);

            return Ok(result);
        }

        private static Models.Indexer.TransactionResponse MapTransactionModelContentToResponse(TransactionContentModel transactionContent)
        {
            var transaction = transactionContent.Transaction;

            return new Models.Indexer.TransactionResponse()
            {
                BlockHash = transaction.BlockHash,
                BlockNumber = (ulong)transaction.BlockNumber,
                BlockTimestamp = transaction.BlockTimestamp,
                ContractAddress = transaction.ContractAddress,
                From = transaction.From,
                Gas = transaction.Gas,
                To = transaction.To,
                GasPrice = transaction.GasPrice,
                GasUsed = transaction.GasUsed,
                Input = transaction.Input,
                Nonce = transaction.Nonce,
                TransactionHash = transaction.TransactionHash,
                TransactionIndex = transaction.TransactionIndex,
                Value = transaction.Value,
                BlockTimeUtc = transaction.BlockTimeUtc,
                HasError = transaction.HasError,
                ErcTransfers = transactionContent.ErcTransfer?.Select(MapErcTransferResponse)
            };
        }

        [HttpPost("history")]
        [ProducesResponseType(typeof(FilteredAddressHistoryResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetAddressHistory([FromBody]EthereumApi.Models.Models.AddressTransactions addressTransactions)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            BusinessModels.AddressTransaction request = new BusinessModels.AddressTransaction()
            {
                Address = addressTransactions.Address,
                Count = addressTransactions.Count,
                Start = addressTransactions.Start,
            };

            IEnumerable<AddressHistoryModel> history = await _ethereumIndexerService.GetAddressHistory(request);
            IEnumerable<AddressHistoryResponse> result = history.Select(item =>
            {
                return new AddressHistoryResponse()
                {
                    BlockNumber = item.BlockNumber,
                    BlockTimestamp = item.BlockTimestamp,
                    BlockTimeUtc = item.BlockTimeUtc,
                    From = item.From,
                    HasError = item.HasError,
                    MessageIndex =item.MessageIndex,
                    To =item.To,
                    TransactionHash = item.TransactionHash,
                    TransactionIndexInBlock = item.TransactionIndexInBlock,
                    Value = item.Value,
                    GasPrice = item.GasPrice,
                    GasUsed = item.GasUsed,
                };
            });

            return Ok(new FilteredAddressHistoryResponse()
            {
                History = result
            });
        }

        [HttpPost("ercHistory")]
        [ProducesResponseType(typeof(FilteredTokenAddressHistoryResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetAddressErcHistory([FromBody]EthereumApi.Models.Models.TokenAddressTransactions addressTransactions)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            BusinessModels.TokenTransaction request = new BusinessModels.TokenTransaction()
            {
                Address = addressTransactions.Address,
                Count = addressTransactions.Count,
                Start = addressTransactions.Start,
                TokenAddress = addressTransactions.TokenAddress
            };

            IEnumerable<ErcAddressHistoryModel> history = await _ethereumIndexerService.GetTokenHistory(request);
            IEnumerable<TokenAddressHistoryResponse> result = history.Select(item =>
            {
                return MapErcTransferResponse(item);
            });

            return Ok(new FilteredTokenAddressHistoryResponse()
            {
                History = result
            });
        }

        private static TokenAddressHistoryResponse MapErcTransferResponse(ErcAddressHistoryModel item)
        {
            return new TokenAddressHistoryResponse()
            {
                ContractAddress = item.ContractAddress,
                BlockNumber = item.BlockNumber,
                BlockTimestamp = item.BlockTimestamp,
                BlockTimeUtc = item.BlockTimeUtc,
                From = item.From,
                HasError = item.HasError,
                MessageIndex = item.MessageIndex,
                To = item.To,
                TransactionHash = item.TransactionHash,
                TransactionIndexInBlock = item.TransactionIndexInBlock,
                TokenTransfered = item.Value,
                GasPrice = item.GasPrice,
                GasUsed = item.GasUsed,
            };
        }
    }
}
