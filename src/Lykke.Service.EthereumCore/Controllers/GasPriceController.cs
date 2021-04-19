﻿using System.Net;
using System.Numerics;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Models.Models;
using Lykke.Service.EthereumCore.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Lykke.Service.EthereumCore.Services;

namespace Lykke.Service.EthereumCore.Controllers
{
    [Route("api/gas-price")]
    [Produces("application/json")]
    public class GasPriceController : Controller
    {
        private readonly IGasPriceService _service;

        public GasPriceController(IGasPriceService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(typeof(GasPriceModel), 200)]
        public async Task<IActionResult> Get()
        {
            var gasPrice = await _service.GetAsync();

            return Ok(new GasPriceModel
            {
                Max = gasPrice.Max.ToString(),
                Min = gasPrice.Min.ToString()
            });
        }

        [HttpPost]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> Post([FromBody] GasPriceModel model)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            var min = BigInteger.Parse(model.Min);
            var max = BigInteger.Parse(model.Max);

            if (min >= max)
            {
                throw new ClientSideException(ExceptionType.WrongParams, "Max gas price should be greater then min");
            }

            await _service.SetAsync(min, max);

            return NoContent();
        }
    }
}
