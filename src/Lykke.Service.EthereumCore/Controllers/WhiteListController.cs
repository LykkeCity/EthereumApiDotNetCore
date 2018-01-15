using Lykke.Service.EthereumCore.BusinessModels;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Models;
using Lykke.Service.EthereumCore.Models.Models;
using Lykke.Service.EthereumCore.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Models.Attributes;

namespace Lykke.Service.EthereumCore.Controllers
{
    [Route("api/whiteList")]
    [Produces("application/json")]
    public class WhiteListController : Controller
    {
        private readonly IWhiteListAddressesRepository _whiteListAddressesRepository;

        public WhiteListController(IWhiteListAddressesRepository whiteListAddressesRepository)
        {
            _whiteListAddressesRepository = whiteListAddressesRepository;
        }

        [HttpGet("{address}")]
        [ProducesResponseType(typeof(EthereumAddressResponse), 200)]
        [ProducesResponseType(typeof(void), 404)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetWhiteListAsync([FromRoute][EthereumAddress] string address)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            var whiteList = await _whiteListAddressesRepository.GetAsync(address);

            if (whiteList == null)
            {
                return NotFound();
            }


            return Ok(new EthereumAddressResponse()
            {
                Address = whiteList.Address
            });
        }

        [HttpPost()]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> AddToWhiteListAsync([FromBody] EthereumAddressRequest model)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            await _whiteListAddressesRepository.SaveAsync(new WhiteListAddress()
            {
                Address = model.Address
            });

            return Ok();
        }

        [HttpDelete("{address}")]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(void), 404)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> DeleteFromWhiteListAsync([FromRoute][EthereumAddress] string address)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            await _whiteListAddressesRepository.DeleteAsync(address);

            return Ok();
        }
    }
}
