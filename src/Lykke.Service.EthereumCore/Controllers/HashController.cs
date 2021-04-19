﻿using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Models;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.EthereumCore.Services.Coins;
using Common.Log;
using System.Numerics;
using System;
using Lykke.Service.EthereumCore.Services;
using Nethereum.Hex.HexConvertors.Extensions;
using Lykke.Service.EthereumCore.Utils;
using Newtonsoft.Json;
using Nethereum.Util;

namespace Lykke.Service.EthereumCore.Controllers
{
    [Route("api/hash")]
    [Produces("application/json")]
    public class HashController : Controller
    {
        private readonly IExchangeContractService _exchangeContractService;
        private readonly ILog _logger;
        private readonly IHashCalculator _hashCalculator;
        private readonly AddressUtil _addressUtil;

        public HashController(IHashCalculator hashCalculator, ILog logger, IExchangeContractService exchangeContractService)
        {
            _addressUtil = new AddressUtil();
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
            //IdCheckResult idCheckResult = await _exchangeContractService.CheckId(guid);
            var amount = BigInteger.Parse(model.Amount);
            byte[] hash;
            try
            {
                hash = _hashCalculator.GetHash(guid, model.CoinAdapterAddress, _addressUtil.ConvertToChecksumAddress(model.FromAddress),
                    _addressUtil.ConvertToChecksumAddress(model.ToAddress), amount);
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync("HashController", "GetHashWithIdAsync", JsonConvert.SerializeObject(model), e, DateTime.UtcNow);
                throw;
            }

            return Ok(new HashResponseWithId
            {
                HashHex = hash.ToHex(),
                OperationId = guid
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
            byte[] hash;

            try
            {
                hash = _hashCalculator.GetHash(model.Id, model.CoinAdapterAddress, _addressUtil.ConvertToChecksumAddress(model.FromAddress),
                  _addressUtil.ConvertToChecksumAddress(model.ToAddress), amount);
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync("HashController", "GetHashAsync", JsonConvert.SerializeObject(model), e, DateTime.UtcNow);
                throw;
            }

            return Ok(new HashResponse { HashHex = hash.ToHex() });
        }
    }
}
