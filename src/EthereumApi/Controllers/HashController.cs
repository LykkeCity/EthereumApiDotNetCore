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
using Services;
using Nethereum.Hex.HexConvertors.Extensions;
using EthereumApi.Utils;
using Newtonsoft.Json;
using Core.Common;

namespace EthereumApi.Controllers
{
    [Route("api/hash")]
    [Produces("application/json")]
    public class HashController : Controller
    {
        private readonly IExchangeContractService _exchangeContractService;
        private readonly ILog _logger;
        private readonly IHashCalculator _hashCalculator;

        public HashController(IHashCalculator hashCalculator, ILog logger, IExchangeContractService exchangeContractService)
        {
            _exchangeContractService = exchangeContractService;
            _hashCalculator = hashCalculator;
            _logger = logger;
        }

        [Route("calculateAndGetId")]
        [HttpPost]
        [ProducesResponseType(typeof(HashResponseWithId), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetHashWithIdAsync([FromBody]BaseCoinRequestParametersModel model)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            var guid = Guid.NewGuid();
            IdCheckResult idCheckResult = await _exchangeContractService.CheckId(guid);
            var amount = BigInteger.Parse(model.Amount);
            var hash = _hashCalculator.GetHash(idCheckResult.ProposedId, model.CoinAdapterAddress, model.FromAddress, model.ToAddress, amount);

            return Ok(new HashResponseWithId
            {
                HashHex = hash.ToHex(),
                OperationId = idCheckResult.ProposedId
            });
        }

        [Route("calculate")]
        [HttpPost]
        [ProducesResponseType(typeof(HashResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetHashAsync([FromBody]BaseCoinRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            var amount = BigInteger.Parse(model.Amount);
            var hash = _hashCalculator.GetHash(model.Id, model.CoinAdapterAddress, model.FromAddress, model.ToAddress, amount);

            return Ok(new HashResponse { HashHex = hash.ToHex() });
        }
    }
}
