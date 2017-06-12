using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Services;
using Nethereum.Web3;

namespace EthereumApi.Controllers
{
    [Route("api/system")]
    [Produces("application/json")]
    public class SystemController : Controller
    {
        //private readonly IContractQueueService _contractQueueService;
        private readonly IContractService _contractService;
        private readonly Web3 _web3;

        public SystemController(IContractService contractService, Web3 web3)
        {
            _web3 = web3;
            _contractService = contractService;
        }

        [Route("amialive")]
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> NewClient()
        {
            // check contract queue
            //var count = await _contractQueueService.Count();

            // check ethereum node
            var block = await _contractService.GetCurrentBlock();
            var currentGasPriceHex = await _web3.Eth.GasPrice.SendRequestAsync();
            return Ok(new { QueueCount = 0, BlockNumber = block.ToString(), Version = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion, CurrentGasPrice = currentGasPriceHex.Value.ToString() });
        }
    }
}
