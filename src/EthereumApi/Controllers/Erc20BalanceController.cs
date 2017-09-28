using BusinessModels;
using BusinessModels.Erc20;
using BusinessModels.PrivateWallet;
using Common.Log;
using Core.Exceptions;
using EthereumApi.Models;
using EthereumApi.Models.Models;
using EthereumApi.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Services.Erc20;
using Services.PrivateWallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EthereumApi.Controllers
{
    [Route("api/erc20Balance")]
    [Produces("application/json")]
    public class Erc20BalanceController : Controller
    {
        private readonly IErc20BalanceService _erc20BalanceService;
        private readonly ILog _log;

        public Erc20BalanceController(IErc20BalanceService erc20BalanceService, ILog log)
        {
            _erc20BalanceService = erc20BalanceService;
            _log = log;
        }

        [HttpPost]
        [ProducesResponseType(typeof(AddressTokenBalanceContainerResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetTransaction([FromBody]GetErcBalance ercTransaction)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            IEnumerable<AddressTokenBalance> addressTokenBalance = 
                await _erc20BalanceService.GetBalancesForAddress(ercTransaction.Address, ercTransaction.TokenAddresses);

            return Ok(new AddressTokenBalanceContainerResponse()
            {
                Balances = addressTokenBalance?.Select(x => new AddressTokenBalanceResponse()
                {
                    Balance           = x.Balance.ToString(),
                    Erc20TokenAddress = x.Erc20TokenAddress,
                    UserAddress       = x.UserAddress
                }),
            });
        }
    }
}
