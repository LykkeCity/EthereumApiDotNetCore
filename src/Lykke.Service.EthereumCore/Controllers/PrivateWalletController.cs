using Lykke.Service.EthereumCore.BusinessModels.PrivateWallet;
using Common.Log;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Models;
using Lykke.Service.EthereumCore.Models.Models;
using Lykke.Service.EthereumCore.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using Lykke.Service.EthereumCore.Services.Transactions;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;

namespace Lykke.Service.EthereumCore.Controllers
{
    [Route("api/privateWallet")]
    [Produces("application/json")]
    public class PrivateWalletController : Controller
    {
        private readonly ITransactionValidationService _transactionValidationService;
        private readonly IPrivateWalletService _privateWalletService;
        private readonly ILog _log;
        private readonly IErc20PrivateWalletService _erc20Service;
        private readonly IOverrideNonceRepository _nonceRepository;

        public PrivateWalletController(IPrivateWalletService privateWalletService, ILog log,
            ITransactionValidationService transactionValidationService,
            IErc20PrivateWalletService erc20Service,
            IOverrideNonceRepository nonceRepository
            )
        {
            _transactionValidationService = transactionValidationService;
            _privateWalletService = privateWalletService;
            _log = log;
            _erc20Service = erc20Service;
            _nonceRepository = nonceRepository;
        }

        [HttpPost("getTransactionWithData")]
        [ProducesResponseType(typeof(EthTransactionRaw), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetTransactionWithDataAsync([FromBody]PrivateWalletDataTransaction ethTransaction)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            string serialized = JsonConvert.SerializeObject(ethTransaction);
            await _log.WriteInfoAsync("PrivateWalletController", "GetTransaction", serialized, "Get transaction for signing", DateTime.UtcNow);
            var transaction = new DataTransaction()
            {
                FromAddress = ethTransaction.FromAddress,
                GasAmount = BigInteger.Parse(ethTransaction.GasAmount),
                GasPrice = BigInteger.Parse(ethTransaction.GasPrice),
                ToAddress = ethTransaction.ToAddress,
                Value = BigInteger.Parse(ethTransaction.Value),
                Data = ethTransaction.Data
            };

            await _privateWalletService.ValidateInputAsync(transaction);
            string transactionHex = await _privateWalletService.GetDataTransactionForSigning(transaction);

            await _log.WriteInfoAsync("PrivateWalletController", "GetTransaction", $"{serialized} + TransactionHex:{transactionHex}",
                "Recieved transaction for signing", DateTime.UtcNow);

            return Ok(new EthTransactionRaw()
            {
                FromAddress = ethTransaction.FromAddress,
                TransactionHex = transactionHex
            });
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

            var nonce = await _nonceRepository.GetNonceAsync(transaction.FromAddress);
            if (string.IsNullOrEmpty(nonce))
            {
                await _log.WriteInfoAsync("PrivateWalletController", "GetTransaction", $"{serialized}",
                    "Check balance", DateTime.UtcNow);

                await _privateWalletService.ValidateInputAsync(transaction);
            }

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

            string transactionHash;
            if (!await _transactionValidationService.IsTransactionErc20Transfer(ethTransactionSigned.SignedTransactionHex))
            {
                transactionHash = await _privateWalletService.SubmitSignedTransaction(ethTransactionSigned.FromAddress,
                    ethTransactionSigned.SignedTransactionHex);
            }
            else
            {
                transactionHash = await _erc20Service.SubmitSignedTransaction(ethTransactionSigned.FromAddress,
                    ethTransactionSigned.SignedTransactionHex);
            }

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

        [HttpPost("overrideNonce")]
        public async Task<IActionResult> OverrideNonce(
            [FromQuery] string address,
            [FromQuery] string nonce)
        {
            if (!BigInteger.TryParse(nonce, out var result))
            {
                return BadRequest(new {Error = "Nonce is not a valid number."});
            }

            await _nonceRepository.AddAsync(address, nonce);
            return Ok();
        }

        [HttpGet("overrideNonce")]
        public async Task<IActionResult> GetAllNonceOverrides()
        {
            var entities = await _nonceRepository.GetAllAsync();
            return Ok(entities);
        }

        [HttpDelete("overrideNonce")]
        public async Task<IActionResult> DeleteOverrideNonce([FromQuery] string address)
        {
            await _nonceRepository.RemoveAsync(address);
            return Ok();
        }
    }
}
