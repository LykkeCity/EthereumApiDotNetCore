using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Core.Exceptions;
using EthereumApi.Models;
using Microsoft.AspNetCore.Mvc;
using Services;
using Services.Coins;
using Common.Log;
using EthereumApiSelfHosted.Models;
using Core.Repositories;

namespace EthereumApi.Controllers
{
    //ForAdminOnly
    [Route("api/externalToken")]
    [Produces("application/json")]
    public class ExternalTokenController : Controller
    {
        private readonly ExternalTokenService _externalTokenService;
        private readonly ILog _logger;

        public ExternalTokenController(ExternalTokenService externalTokenService, ILog logger)
        {
            _externalTokenService = externalTokenService;
            _logger = logger;
        }

        [Route("create")]
        [HttpPost]
        [Produces(typeof(TransactionResponse))]
        public async Task<IActionResult> CreateTransferContract([FromBody]CreateExternalTokenModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IExternalToken token = await _externalTokenService.CreateExternalToken(model.TokenName);

            return Ok(new ExternalTokenModel
            {
                ContractAddress = token.ContractAddress,
                Id = token.Id,
                Name = token.Name,
            });
        }
    }
}
