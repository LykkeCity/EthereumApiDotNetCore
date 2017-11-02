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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Linq;
using Services.Model;
using Common;
using Newtonsoft.Json;
using EthereumApi.Utils;

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
        [ProducesResponseType(typeof(OperationIdResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> Cashout([FromBody]CashoutModel model)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            await Log("Cashout", $"Begin Process {this.GetIp()}", model);

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
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
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
        [ProducesResponseType(typeof(OperationIdResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> Transfer([FromBody] TransferModel model)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            await Log("Transfer", $"Begin Process {this.GetIp()}", model);

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
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            await Log("CheckSign", $"Begin Process {this.GetIp()}", model);

            BigInteger amount = BigInteger.Parse(model.Amount);
            var result = _exchangeContractService.CheckSign(model.Id, model.CoinAdapterAddress,
                _addressUtil.ConvertToChecksumAddress(model.FromAddress), _addressUtil.ConvertToChecksumAddress(model.ToAddress), amount, model.Sign);

            await Log("CheckSign", "End Process", model, result.ToString());

            return Ok(new CheckSignResponse { SignIsCorrect = result });
        }

        [Route("transferWithChange")]
        [HttpPost]
        [ProducesResponseType(typeof(OperationIdResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> TransferWithChange([FromBody] TransferWithChangeModel model)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }
            
            await Log("TransferWithChange", $"Begin Process {this.GetIp()}", model);

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
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            var isSynced = await _exchangeContractService.CheckLastTransactionCompleted(model.CoinAdapterAddress, _addressUtil.ConvertToChecksumAddress(model.UserAddress));

            return Ok(new CheckPendingResponse { IsSynced = isSynced });
        }

        [Route("estimateCashoutGas")]
        [HttpPost]
        [ProducesResponseType(typeof(EstimatedGasModel), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> EstimateCashoutGas([FromBody]TransferModel model)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            BigInteger amount = BigInteger.Parse(model.Amount);
            OperationEstimationResult cashoutEstimationResult = await _exchangeContractService.EstimateCashoutGas(model.Id, model.CoinAdapterAddress,
                _addressUtil.ConvertToChecksumAddress(model.FromAddress), _addressUtil.ConvertToChecksumAddress(model.ToAddress), amount, model.Sign);

            await _logger.WriteInfoAsync("ExchangeController", "EstimateCashoutGas", 
                model.ToJson(), $"Estimated amount:{cashoutEstimationResult.GasAmount}", DateTime.UtcNow);

            return Ok(new EstimatedGasModel
            {
                EstimatedGas = cashoutEstimationResult.GasAmount.ToString(),
                IsAllowed = cashoutEstimationResult.IsAllowed
            });
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

    public static class CtxExtension
    {
        public static string GetIp(this Controller ctx)
        {
            string ip = string.Empty;

            // http://stackoverflow.com/a/43554000/538763
            var xForwardedForVal = GetHeaderValueAs<string>(ctx.HttpContext, "X-Forwarded-For").SplitCsv().FirstOrDefault();

            if (!string.IsNullOrEmpty(xForwardedForVal))
            {
                ip = xForwardedForVal.Split(':')[0];
            }

            // RemoteIpAddress is always null in DNX RC1 Update1 (bug).
            if (string.IsNullOrWhiteSpace(ip) && ctx.HttpContext?.Connection?.RemoteIpAddress != null)
                ip = ctx.HttpContext.Connection.RemoteIpAddress.ToString();

            if (string.IsNullOrWhiteSpace(ip))
                ip = GetHeaderValueAs<string>(ctx.HttpContext, "REMOTE_ADDR");

            return ip;
        }

        private static T GetHeaderValueAs<T>(HttpContext httpContext, string headerName)
        {
            StringValues values;

            if (httpContext?.Request?.Headers?.TryGetValue(headerName, out values) ?? false)
            {
                string rawValues = values.ToString();   // writes out as Csv when there are multiple.

                if (!string.IsNullOrEmpty(rawValues))
                    return (T)Convert.ChangeType(values.ToString(), typeof(T));
            }
            return default(T);
        }

        private static List<string> SplitCsv(this string csvList, bool nullOrWhitespaceInputReturnsNull = false)
        {
            if (string.IsNullOrWhiteSpace(csvList))
                return nullOrWhitespaceInputReturnsNull ? null : new List<string>();

            return csvList
                .TrimEnd(',')
                .Split(',')
                .AsEnumerable<string>()
                .Select(s => s.Trim())
                .ToList();
        }
    }
}

