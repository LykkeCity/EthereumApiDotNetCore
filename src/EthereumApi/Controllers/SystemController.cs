using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace EthereumApi.Controllers
{
	[Route("api/system")]
	[Produces("application/json")]
	public class SystemController : Controller
	{
		private readonly IContractQueueService _contractQueueService;
		private readonly IContractService _contractService;

		public SystemController(IContractQueueService contractQueueService, IContractService contractService)
		{
			_contractQueueService = contractQueueService;
			_contractService = contractService;
		}

		[Route("amialive")]
		[HttpGet]
		[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
		public async Task<IActionResult> NewClient()
		{
			// check contract queue
			var count = await _contractQueueService.Count();

			// check ethereum node
			var block = await _contractService.GetCurrentBlock();

			return Ok(new { QueueCount = count, BlockNumber = block.ToString(), Version = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion });
		}
	}
}
