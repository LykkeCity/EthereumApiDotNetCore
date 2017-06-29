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
    [Route("api/rpc")]
    [Produces("application/json")]
    public class RpcController : Controller
    {
        private readonly IEthereumIndexerService _ethereumIndexerService;

        public RpcController(IEthereumIndexerService ethereumIndexerService)
        {
            _ethereumIndexerService = ethereumIndexerService;
        }

        [HttpGet("getBalance/{address}")]
        [ProducesResponseType(typeof(BalanceModel), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetBalanceAsync([FromRoute] string address)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            BigInteger balance = await _ethereumIndexerService.GetEthBalance(address);

            return Ok(new BalanceModel() {
                Amount = balance.ToString()
            });
        }
    }
}
