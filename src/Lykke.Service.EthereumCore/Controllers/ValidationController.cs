using Common.Log;
using Lykke.Service.EthereumCore.Models.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
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
                IsValid = _exchangeContractService.IsValidAddress(ethAddress ?? "")
            });
        }
    }
}
