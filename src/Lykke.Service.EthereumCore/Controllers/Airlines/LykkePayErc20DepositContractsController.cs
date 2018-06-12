using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using EthereumApi.Models.Models.Airlines;
using Lykke.Service.EthereumCore.Attributes;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCoreSelfHosted.Models;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.EthereumCore.Services;
using Lykke.Service.EthereumCore.Models;
using Lykke.Service.EthereumCore.Utils;
using Newtonsoft.Json;

namespace Lykke.Service.EthereumCore.Controllers.Airlines
{
    [ApiKeyAuthorize] //Configure keys in settings
    [Route("api/lykke-pay/erc20deposits")]
    [Produces("application/json")]
    public class AirlinesErc20DepositContractsController : Controller
    {
        private readonly IErc20DepositContractService _contractService;

        public AirlinesErc20DepositContractsController([KeyFilter(Constants.AirLinesKey)] IErc20DepositContractService contractService)
        {
            _contractService = contractService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(RegisterResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(void), 401)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> CreateDepositContractAsync([FromQuery][Required] string userAddress)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            var contractAddress = await _contractService.AssignContract(userAddress);

            return Ok(new RegisterResponse
            {
                Contract = contractAddress
            });
        }

        [HttpGet]
        [ProducesResponseType(typeof(RegisterResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(void), 401)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetDepositContractAsync([FromQuery][Required] string userAddress)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            var contractAddress = await _contractService.GetContractAddress(userAddress);

            return Ok(new RegisterResponse
            {
                Contract = contractAddress
            });
        }

        //TODO: Contract response
        [HttpPost("transfer")]
        [ProducesResponseType(typeof(OperationIdResponse), 200)] 
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(void), 401)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> TransferAsync([FromBody] TransferFromDepositRequest request)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            string opId = await _contractService.RecievePaymentFromDepositContract(request.DepositContractAddress?.ToLower(), 
                request.TokenAddress?.ToLower(), 
                request.DestinationAddress?.ToLower(),
                request.TokenAmount);

            return Ok(new OperationIdResponse()
            {
                OperationId = opId
            });
        }
    }
}