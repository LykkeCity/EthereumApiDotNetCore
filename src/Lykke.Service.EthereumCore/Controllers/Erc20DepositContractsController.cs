using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCoreSelfHosted.Models;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.EthereumCore.Services;

namespace Lykke.Service.EthereumCore.Controllers
{
    [Route("api/erc20deposits")]
    [Produces("application/json")]
    public class Erc20DepositContractsController : Controller
    {
        private readonly IErc20DepositContractService _contractService;
        private readonly IErc20ContracAssigner _erc20ContracAssigner;

        public Erc20DepositContractsController(
            [KeyFilter(Constants.DefaultKey)]IErc20DepositContractService contractService,
            [KeyFilter(Constants.DefaultKey)]IErc20ContracAssigner erc20ContracAssigner)
        {
            _contractService = contractService;
            _erc20ContracAssigner = erc20ContracAssigner;
        }


        [HttpPost]
        [ProducesResponseType(typeof(RegisterResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> CreateDepositContract([FromQuery] string userAddress)
        {
            var contractAddress = await _erc20ContracAssigner.AssignContract(userAddress);

            return Ok(new RegisterResponse
            {
                Contract = contractAddress
            });
        }

        [HttpGet]
        [ProducesResponseType(typeof(RegisterResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetDepositContract([FromQuery] string userAddress)
        {
            var contractAddress = await _contractService.GetContractAddress(userAddress);

            return Ok(new RegisterResponse
            {
                Contract = contractAddress
            });
        }

        [HttpPost("recieve-payment-without-estimation")]
        [ProducesResponseType(typeof(ReceivePaymentWithoutEstimationResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> RecievePaymentFromDepositContractWithoutEstimation([FromBody] ReceivePaymentWithoutEstimationRequest request)
        {
            var transactionHash = await _contractService.RecievePaymentFromDepositContractWithoutEstimation(
                request.DepositContractAddress,
                request.Erc20TokenContractAddress,
                request.ToAddress,
                request.Gas);

            return Ok(new ReceivePaymentWithoutEstimationResponse
            {
                TransactionHash = transactionHash
            });
        }
    }
}