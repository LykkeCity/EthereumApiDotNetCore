using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Core.PrivateWallet;
using Lykke.Service.EthereumCore.Models;
using Lykke.Service.EthereumCore.Models.Models;
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using Lykke.Service.EthereumCore.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Numerics;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Controllers
{
    [Route("api/estimation")]
    [Produces("application/json")]
    public class EstimationController : Controller
    {
        private readonly IEstimationService _estimationService;
        private readonly IErc20PrivateWalletService _erc20PrivateWalletService;

        public EstimationController(IEstimationService estimationService,
            IErc20PrivateWalletService erc20PrivateWalletService)
        {
            _estimationService = estimationService;
            _erc20PrivateWalletService = erc20PrivateWalletService;
        }

        [HttpPost("estimateTransaction")]
        [ProducesResponseType(typeof(EstimatedGasModelV2), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> EstimateTransaction([FromBody]PrivateWalletEstimateTransaction estimateTransaction)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            var executionCost = await _estimationService.EstimateTransactionExecutionCostAsync(
                estimateTransaction.FromAddress,
                estimateTransaction.ToAddress,
                BigInteger.Parse(estimateTransaction.EthAmount),
                BigInteger.Parse(estimateTransaction.GasPrice),
                null);

            return Ok(new EstimatedGasModelV2()
            {
                EstimatedGas = executionCost.GasAmount.ToString(),
                GasPrice = executionCost.GasPrice.ToString(),
                EthAmount = executionCost.EthAmount.ToString(),
                IsAllowed = executionCost.IsAllowed,
            });
        }

        [HttpPost("estimateTransaction/erc20")]
        [ProducesResponseType(typeof(EstimatedGasModelV2), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> EstimateErc20Transaction([FromBody]PrivateWalletErc20EstimateTransaction estimateTransaction)
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

            var functionCall = _erc20PrivateWalletService.GetTransferFunctionCallEncoded(
                estimateTransaction.TokenAddress,
                estimateTransaction.ToAddress,
                BigInteger.Parse(estimateTransaction.TokenAmount)
            );
            var executionCost = await _estimationService.EstimateTransactionExecutionCostAsync(
                estimateTransaction.FromAddress,
                estimateTransaction.TokenAddress,
                BigInteger.Parse(estimateTransaction.EthAmount),
                BigInteger.Parse(estimateTransaction.GasPrice),
                functionCall);

            return Ok(new EstimatedGasModelV2
            {
                EstimatedGas = executionCost.GasAmount.ToString(),
                GasPrice = executionCost.GasPrice.ToString(),
                EthAmount = executionCost.EthAmount.ToString(),
                IsAllowed = executionCost.IsAllowed,
            });
        }
    }
}
