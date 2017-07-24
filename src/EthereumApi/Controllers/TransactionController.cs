﻿using BusinessModels;
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

            BusinessModels.AddressTransactions request = new BusinessModels.AddressTransactions()
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

            BusinessModels.AddressTransactions request = new BusinessModels.AddressTransactions()
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
                    Value = item.Value
                };
            });

            return Ok(new FilteredAddressHistoryResponse()
            {
                History = result
            });
        }
    }
}
