﻿using BusinessModels;
using Common.Log;
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
        private readonly ILog _log;

        public PrivateWalletController(IPrivateWalletService privateWalletService, ILog log)
        {
            _privateWalletService = privateWalletService;
            _log = log;
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

            string serialized = JsonConvert.SerializeObject(ethTransaction);
            await _log.WriteInfoAsync("PrivateWalletController", "GetTransaction", serialized, "Get transaction for signing", DateTime.UtcNow);
            var transaction = new EthTransaction()
            {
                FromAddress = ethTransaction.FromAddress,
                GasAmount = BigInteger.Parse(ethTransaction.GasAmount),
                GasPrice = BigInteger.Parse(ethTransaction.GasPrice),
                ToAddress = ethTransaction.ToAddress,
                Value = BigInteger.Parse(ethTransaction.Value)
            };

            await _privateWalletService.ValidateInputAsync(transaction);
            string transactionHex = await _privateWalletService.GetTransactionForSigning(transaction);

            await _log.WriteInfoAsync("PrivateWalletController", "GetTransaction", $"{serialized} + TransactionHex:{transactionHex}",
                "Recieved transaction for signing", DateTime.UtcNow);

            return Ok(new EthTransactionRaw()
            {
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

            string serialized = JsonConvert.SerializeObject(ethTransactionSigned);
            await _log.WriteInfoAsync("PrivateWalletController", "SubmitSignedTransaction", serialized
                , "StartSubmitSignedTransaction", DateTime.UtcNow);

            string transactionHash = await _privateWalletService.SubmitSignedTransaction(ethTransactionSigned.FromAddress, ethTransactionSigned.SignedTransactionHex);

            await _log.WriteInfoAsync("PrivateWalletController", "SubmitSignedTransaction",
                $"{serialized}-TransactionHash:{transactionHash}", "EndSubmitSignedTransaction", DateTime.UtcNow);

            return Ok(new HashResponse()
            {
                HashHex = transactionHash
            });
        }

        [HttpPost("estimateTransaction")]
        [ProducesResponseType(typeof(EstimatedGasModel), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> EstimateSignedTransaction([FromBody]PrivateWalletEthSignedTransaction ethTransactionSigned)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            string serialized = JsonConvert.SerializeObject(ethTransactionSigned);
            await _log.WriteInfoAsync("PrivateWalletController", "EstimateSignedTransaction", serialized
                , "StartEstimateSignedTransaction", DateTime.UtcNow);

            var executionCost = await _privateWalletService.EstimateTransactionExecutionCost(ethTransactionSigned.FromAddress, ethTransactionSigned.SignedTransactionHex);

            await _log.WriteInfoAsync("PrivateWalletController", "EstimateSignedTransaction",
                $"{serialized}-TransactionHash:{executionCost.GasAmount.ToString()}", "EndEstimateSignedTransaction", DateTime.UtcNow);

            return Ok(new EstimatedGasModel
            {
                EstimatedGas = executionCost.GasAmount.ToString(),
                IsAllowed = executionCost.IsAllowed
            });
        }
    }
}
