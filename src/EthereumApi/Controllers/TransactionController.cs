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

            BusinessModels.AddressTransactions request = new BusinessModels.AddressTransactions()
            {
                Address = addressTransactions.Address,
                Count = addressTransactions.Count,
                Start = addressTransactions.Start,
            };

            IEnumerable<TransactionContentModel> transactions = await _ethereumIndexerService.GetTransactionHistory(request);
            IEnumerable<Models.Indexer.TransactionResponse> result = transactions.Select(transactionContent =>
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
                };
            });

            return Ok(new FilteredTransactionsResponse()
            {
                Transactions = result
            });
        }
    }
}
