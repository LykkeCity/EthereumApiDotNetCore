using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Core.Exceptions;
using EthereumApi.Models;
using Microsoft.AspNetCore.Mvc;
using Services.Coins;
using Common.Log;
using System.Numerics;
using System;
using Nethereum.Util;
using Services;

namespace EthereumApi.Controllers
{
    [Route("api/exchange")]
    [Produces("application/json")]
    public class ExchangeController : Controller
    {
        private readonly IExchangeContractService _exchangeContractService;
        private readonly ILog _logger;
        private AddressUtil _addressUtil;
        private readonly IPendingOperationService _pendingOperationService;

        public ExchangeController(IExchangeContractService exchangeContractService, ILog logger, IPendingOperationService pendingOperationService)
        {
            _addressUtil = new AddressUtil();
            _exchangeContractService = exchangeContractService;
            _logger = logger;
            _pendingOperationService = pendingOperationService;
        }

        [Route("cashout")]
        [HttpPost]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> Cashout([FromBody]CashoutModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await Log("Cashout", "Begin Process", model);

            var amount = BigInteger.Parse(model.Amount);
            var operationId = await _pendingOperationService.CashOut(model.Id, model.CoinAdapterAddress,
                _addressUtil.ConvertToChecksumAddress(model.FromAddress), _addressUtil.ConvertToChecksumAddress(model.ToAddress), amount, model.Sign);

            await Log("Cashout", "End Process", model, operationId);

            return Ok(new OperationIdResponse { OperationId = operationId });
        }

        [Route("checkId/{guid}")]
        [HttpGet]
        [ProducesResponseType(typeof(CheckIdResponse), 200)]
        public async Task<IActionResult> CheckId([FromRoute]Guid guid)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _exchangeContractService.CheckId(guid);

            return Ok(new CheckIdResponse
            {
                IsOk = result.IsFree,
                ProposedId = result.ProposedId,
            });
        }

        [Route("transfer")]
        [HttpPost]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> Transfer([FromBody] TransferModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await Log("Transfer", "Begin Process", model);

            BigInteger amount = BigInteger.Parse(model.Amount);
            var operationId = await _pendingOperationService.Transfer(model.Id, model.CoinAdapterAddress,
                _addressUtil.ConvertToChecksumAddress(model.FromAddress), _addressUtil.ConvertToChecksumAddress(model.ToAddress), amount, model.Sign);

            await Log("Transfer", "End Process", model, operationId);

            return Ok(new OperationIdResponse { OperationId = operationId });
        }

        [Route("checkSign")]
        [HttpPost]
        [Produces(typeof(CheckSignResponse))]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> CheckSign([FromBody] CheckSignModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await Log("TransferWithChange", "Begin Process", model);

            BigInteger amount = BigInteger.Parse(model.Amount);
            var result = _exchangeContractService.CheckSign(model.Id, model.CoinAdapterAddress,
                _addressUtil.ConvertToChecksumAddress(model.FromAddress), _addressUtil.ConvertToChecksumAddress(model.ToAddress), amount, model.Sign);

            await Log("TransferWithChange", "End Process", model, result.ToString());

            return Ok(new CheckSignResponse { SignIsCorrect = result });
        }

        [Route("transferWithChange")]
        [HttpPost]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> TransferWithChange([FromBody] TransferWithChangeModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await Log("TransferWithChange", "Begin Process", model);

            BigInteger amount = BigInteger.Parse(model.Amount);
            BigInteger change = BigInteger.Parse(model.Change);
            var operationId = await _pendingOperationService.TransferWithChange(model.Id, model.CoinAdapterAddress, 
                _addressUtil.ConvertToChecksumAddress(model.FromAddress), _addressUtil.ConvertToChecksumAddress(model.ToAddress),
                amount, model.SignFrom, change, model.SignTo);

            await Log("TransferWithChange", "End Process", model, operationId);

            return Ok(new OperationIdResponse { OperationId = operationId });
        }

        [Route("checkPendingTransaction")]
        [HttpPost]
        [ProducesResponseType(typeof(CheckPendingResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> CheckPendingTransactions([FromBody] CheckPendingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var isSynced = await _exchangeContractService.CheckLastTransactionCompleted(model.CoinAdapterAddress, _addressUtil.ConvertToChecksumAddress(model.UserAddress));

            return Ok(new CheckPendingResponse { IsSynced = isSynced });
        }

        private async Task Log(string method, string status, object model, string transaction = "")
        {
            var properties = model.GetType().GetTypeInfo().GetProperties();
            var builder = new StringBuilder();
            foreach (var prop in properties)
                builder.Append($"{prop.Name}: [{prop.GetValue(model)}], ");

            if (!string.IsNullOrWhiteSpace(transaction))
                builder.Append($"Transaction: [{transaction}]");

            await _logger.WriteInfoAsync("CoinController", method, status, builder.ToString());
        }
    }
}
