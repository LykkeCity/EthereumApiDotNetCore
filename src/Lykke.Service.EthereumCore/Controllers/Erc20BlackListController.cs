using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Models.Attributes;
using Lykke.Service.EthereumCore.Models.Models;
using Lykke.Service.EthereumCore.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Controllers
{
    [Route("api/erc20BlackList")]
    [Produces("application/json")]
    public class Erc20BlackListController : Controller
    {
        private readonly IErc20BlackListAddressesRepository _erc20BlackListAddressesRepository;

        public Erc20BlackListController(IErc20BlackListAddressesRepository erc20BlackListAddressesRepository)
        {
            _erc20BlackListAddressesRepository = erc20BlackListAddressesRepository;
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

            var blackList = await _erc20BlackListAddressesRepository.GetAsync(address);

            if (blackList == null)
            {
                return NotFound();
            }


            return Ok(new EthereumAddressResponse()
            {
                Address = blackList.Address
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

            await _erc20BlackListAddressesRepository.SaveAsync(new Erc20BlackListAddress()
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

            await _erc20BlackListAddressesRepository.DeleteAsync(address);

            return Ok();
        }
    }
}
