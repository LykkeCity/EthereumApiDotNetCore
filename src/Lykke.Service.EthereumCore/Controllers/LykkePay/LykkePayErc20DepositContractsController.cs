using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using EthereumApi.Models.Models.LykkePay;
using Lykke.Service.EthereumCore.Attributes;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCoreSelfHosted.Models;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.EthereumCore.Services;

namespace Lykke.Service.EthereumCore.Controllers.LykkePay
{
    [ApiKeyAuthorize] //Configure keys in settings
    [Route("api/lykke-pay/erc20deposits")]
    [Produces("application/json")]
    public class LykkePayErc20DepositContractsController : Controller
    {
        private readonly IErc20DepositContractService _contractService;

        public LykkePayErc20DepositContractsController([KeyFilter(Constants.LykkePayKey)] IErc20DepositContractService contractService)
        {
            _contractService = contractService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(RegisterResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        //[ProducesResponseType(typeof(UnauthorizedResult), 401)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> CreateDepositContractAsync([FromQuery] string userAddress)
        {
            var contractAddress = await _contractService.AssignContract(userAddress);

            return Ok(new RegisterResponse
            {
                Contract = contractAddress
            });
        }

        [HttpGet]
        [ProducesResponseType(typeof(RegisterResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        //[ProducesResponseType(typeof(UnauthorizedResult), 401)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetDepositContractAsync([FromQuery] string userAddress)
        {
            var contractAddress = await _contractService.GetContractAddress(userAddress);

            return Ok(new RegisterResponse
            {
                Contract = contractAddress
            });
        }

        [HttpPost("transfer")]
        [ProducesResponseType(typeof(RegisterResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        //[ProducesResponseType(typeof(UnauthorizedResult), 401)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> TransferAsync([FromBody] TransferFromDepositRequest request)
        {
            var contractAddress = await _contractService.GetContractAddress(request.UserAddress);

            if (contractAddress != null)
            {
                await _contractService.RecievePaymentFromDepositContract(contractAddress, request.TokenAddress, request.DestinationAddress);
            }

            return Ok(new RegisterResponse
            {
                Contract = contractAddress
            });
        }
    }
}