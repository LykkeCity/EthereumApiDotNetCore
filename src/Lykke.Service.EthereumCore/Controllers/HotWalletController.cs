using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Models.Models;
using Lykke.Service.EthereumCore.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Lykke.Service.EthereumCore.Services.HotWallet;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Controllers
{
    [Route("api/hotWallet")]
    [Produces("application/json")]
    public class HotWalletController : Controller
    {
        private readonly IHotWalletService _hotWalletService;

        public HotWalletController(IHotWalletService hotWalletService)
        {
            _hotWalletService = hotWalletService;
        }

        /// <summary>
        /// Cashout from hot wallet
        /// </summary>
        /// <param name="HotWalletCashout"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> CashoutAsync([FromBody]HotWalletCashoutRequest hotWalletCashout)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }
            
            await _hotWalletService.EnqueueCashoutAsync(new Lykke.Service.EthereumCore.Core.Repositories.HotWalletOperation()
            {
                Amount = BigInteger.Parse(hotWalletCashout.Amount),
                FromAddress = hotWalletCashout.FromAddress,
                OperationId = hotWalletCashout.OperationId,
                ToAddress = hotWalletCashout.ToAddress,
                TokenAddress = hotWalletCashout.TokenAddress
            });

            return Ok();
        }
    }
}
