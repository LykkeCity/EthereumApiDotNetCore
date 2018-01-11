using Lykke.Service.EthereumCore.BusinessModels;
using Lykke.Service.EthereumCore.BusinessModels.PrivateWallet;
using Common.Log;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Models;
using Lykke.Service.EthereumCore.Models.Models;
using Lykke.Service.EthereumCore.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Controllers
{
    [Route("api/erc20Wallet")]
    [Produces("application/json")]
    public class Erc20WalletController : Controller
    {
        private readonly ILog _log;
        private IErc20PrivateWalletService _erc20Service;

        public Erc20WalletController(IErc20PrivateWalletService erc20Service, ILog log)
        {
            _erc20Service = erc20Service;
            _log = log;
        }

        [HttpPost("getTransaction")]
        [ProducesResponseType(typeof(EthTransactionRaw), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetTransaction([FromBody]PrivateWalletErc20Transaction ercTransaction)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            string serialized = JsonConvert.SerializeObject(ercTransaction);
            await _log.WriteInfoAsync("PrivateWalletController", "GetTransaction", serialized, "Get transaction for signing", DateTime.UtcNow);

            Erc20Transaction transaction = new Erc20Transaction()
            {
                TokenAddress = ercTransaction.TokenAddress,
                TokenAmount = BigInteger.Parse(ercTransaction.TokenAmount),
                FromAddress = ercTransaction.FromAddress,
                GasAmount = BigInteger.Parse(ercTransaction.GasAmount),
                GasPrice = BigInteger.Parse(ercTransaction.GasPrice),
                ToAddress = ercTransaction.ToAddress,
                Value = BigInteger.Parse(ercTransaction.Value),
            };
            await _erc20Service.ValidateInput(transaction);
            string transactionHex = await _erc20Service.GetTransferTransactionRaw(transaction);

            await _log.WriteInfoAsync("PrivateWalletController", "GetTransaction", $"{serialized} + TransactionHex:{transactionHex}",
                "Recieved transaction for signing", DateTime.UtcNow);

            return Ok(new EthTransactionRaw()
            {
                FromAddress = ercTransaction.FromAddress,
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
            await _log.WriteInfoAsync("Erc20WalletController", "SubmitSignedTransaction", serialized
                , "StartSubmitSignedTransaction", DateTime.UtcNow);

            string transactionHash = await _erc20Service.SubmitSignedTransaction(ethTransactionSigned.FromAddress, ethTransactionSigned.SignedTransactionHex);

            await _log.WriteInfoAsync("Erc20WalletController", "SubmitSignedTransaction",
                $"{serialized}-TransactionHash:{transactionHash}", "EndSubmitSignedTransaction", DateTime.UtcNow);

            return Ok(new HashResponse()
            {
                HashHex = transactionHash
            });
        }
    }
}
