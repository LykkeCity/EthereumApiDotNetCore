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

namespace EthereumApi.Controllers
{
    [Route("api/exchange")]
    [Produces("application/json")]
    public class ExchangeController : Controller
    {
        private readonly IExchangeContractService _exchangeContractService;
        private readonly ILog _logger;

        public ExchangeController(IExchangeContractService exchangeContractService, ILog logger)
        {
            _exchangeContractService = exchangeContractService;
            _logger = logger;
        }

        //[Route("swap")]
        //[HttpPost]
        //[Produces(typeof(TransactionResponse))]
        //public async Task<IActionResult> Swap([FromBody]SwapModel model)
        //{
        //    if (!ModelState.IsValid)
        //        throw new BackendException(BackendExceptionType.MissingRequiredParams);

        //    await Log("Swap", "Begin Process", model);

        //    var transaction = await _coinContractService.Swap(model.Id, model.ClientA, model.ClientB, model.CoinA, model.CoinB,
        //        model.AmountA, model.AmountB, model.SignA, model.SignB);

        //    await Log("Swap", "End Process", model, transaction);

        //    return Ok(new TransactionResponse { TransactionHash = transaction });
        //}

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
            var transaction = await _exchangeContractService.CashOut(model.Id, model.CoinAdapterAddress, model.FromAddress, model.ToAddress, amount, model.Sign);

            await Log("Cashout", "End Process", model, transaction);

            return Ok(new TransactionResponse { TransactionHash = transaction });
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
            var transaction = await _exchangeContractService.Transfer(model.Id, model.CoinAdapterAddress, model.FromAddress, model.ToAddress, amount, model.Sign);

            await Log("Transfer", "End Process", model, transaction);

            return Ok(new TransactionResponse { TransactionHash = transaction });
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
                model.FromAddress, model.ToAddress, amount, model.Sign);

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
            var transaction = await _exchangeContractService.TransferWithChange(model.Id, model.CoinAdapterAddress, model.FromAddress, 
                model.ToAddress, amount, model.SignFrom, change, model.SignTo);

            await Log("TransferWithChange", "End Process", model, transaction);

            return Ok(new TransactionResponse { TransactionHash = transaction });
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
