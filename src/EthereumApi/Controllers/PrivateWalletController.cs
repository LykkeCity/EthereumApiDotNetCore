using BusinessModels;
using Core.Exceptions;
using EthereumApi.Models;
using EthereumApi.Models.Models;
using EthereumApi.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Services.PrivateWallet;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EthereumApi.Controllers
{
    [Route("api/privateWallet")]
    [Produces("application/json")]
    public class PrivateWalletController : Controller
    {
        private IPrivateWalletService _privateWalletService;

        public PrivateWalletController(IPrivateWalletService privateWalletService)
        {
            _privateWalletService = privateWalletService;
        }

        [HttpPost("getTransaction")]
        [ProducesResponseType(typeof(EthTransactionRaw), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetTransaction([FromBody]PrivateWalletEthTransaction ethTransaction)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            string transactionHex = await _privateWalletService.GetTransactionForSigning(new EthTransaction()
            {
                FromAddress = ethTransaction.FromAddress,
                GasAmount = BigInteger.Parse(ethTransaction.GasAmount),
                GasPrice = BigInteger.Parse(ethTransaction.GasPrice),
                ToAddress = ethTransaction.ToAddress,
                Value = BigInteger.Parse(ethTransaction.Value)
            });

            return Ok(new EthTransactionRaw() {
                FromAddress = ethTransaction.FromAddress,
                TransactionHex = transactionHex
            });
        }

        [HttpPost("submitTransaction")]
        [ProducesResponseType(typeof(HashResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> SubmitSignedTransaction([FromBody]PrivateWalletEthSignedTransaction ethTransactionSigned)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            string transactionHash = await _privateWalletService.SubmitSignedTransaction(ethTransactionSigned.FromAddress, ethTransactionSigned.SignedTransactionHex);

            return Ok(new HashResponse()
            {
                HashHex = transactionHash
            });
        }
    }
}
