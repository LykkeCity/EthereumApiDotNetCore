using Lykke.Service.EthereumCore.BusinessModels;
using Lykke.Service.EthereumCore.BusinessModels.Erc20;
using Lykke.Service.EthereumCore.BusinessModels.PrivateWallet;
using Common.Log;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Models;
using Lykke.Service.EthereumCore.Models.Models;
using Lykke.Service.EthereumCore.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Lykke.Service.EthereumCore.Services.Erc20;
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore;
using Lykke.Service.EthereumCore.Services.Coins;

namespace Lykke.Service.EthereumCore.Controllers
{
    [Route("api/validation")]
    [Produces("application/json")]
    public class ValidationController : Controller
    {
        private readonly IExchangeContractService _exchangeContractService;
        private readonly ILog _log;

        public ValidationController(IExchangeContractService exchangeContractService, ILog log)
        {
            _exchangeContractService = exchangeContractService;
            _log = log;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IsAddressValidResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> ValidateAsync([FromQuery]string ethAddress)
        {
            return Ok(new IsAddressValidResponse()
            {
                IsValid = _exchangeContractService.IsValidAddress(ethAddress)
            });
        }
    }
}
