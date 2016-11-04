using System.Threading.Tasks;
using Core.Exceptions;
using EthereumApi.Models;
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
        public async Task<IActionResult> NewClientOld()
        {
            var contract = await _contractQueueService.GetContractAndSave(null);

            var response = new RegisterResponse
            {
                Contract = contract
            };

            return Ok(response);
        }

        [Route("register/{userWallet}")]
        [HttpGet]
        [Produces(typeof(RegisterResponse))]
        public async Task<IActionResult> NewClient(string userWallet)
        {
            if (string.IsNullOrWhiteSpace(userWallet))
                throw new BackendException(BackendExceptionType.MissingRequiredParams);

            var contract = await _contractQueueService.GetContractAndSave(userWallet);

            var response = new RegisterResponse
            {
                Contract = contract
            };

            return Ok(response);
        }

        [Route("addWallet/{userWallet}")]
        [HttpPost]
        public async Task<IActionResult> AddUserWallet([FromBody]AddWalletModel model)
        {
            if (!ModelState.IsValid)
                throw new BackendException(BackendExceptionType.MissingRequiredParams);

            await _contractQueueService.UpdateUserWallet(model.UserContract, model.UserWallet);

            return Ok();
        }
    }
}
