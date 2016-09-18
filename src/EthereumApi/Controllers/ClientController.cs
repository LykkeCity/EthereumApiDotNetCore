using System.Threading.Tasks;
using EthereumApiSelfHosted.Models;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace EthereumApi.Controllers
{
	[Route("api/client")]
	[Produces("application/json")]
	public class ClientController : Controller
	{
		private readonly IContractQueueService _contractQueueService;

		public ClientController(IContractQueueService contractQueueService)
		{
			_contractQueueService = contractQueueService;
		}

		[Route("register")]
		[HttpPost]
		[Produces(typeof(RegisterResponse))]
		public async Task<IActionResult> NewClient()
		{
			var contract = await _contractQueueService.GetContract();

			var response = new RegisterResponse
			{
				Contract = contract
			};

			return Ok(response);
		}
	}
}
