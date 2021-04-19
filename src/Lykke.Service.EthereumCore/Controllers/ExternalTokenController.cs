using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Models;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.EthereumCore.Services;
using Common.Log;
using Lykke.Service.EthereumCoreSelfHosted.Models;
using Lykke.Service.EthereumCore.Core.Repositories;
using System.Numerics;

namespace Lykke.Service.EthereumCore.Controllers
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

        [HttpGet]
        [Produces(typeof(ListResult<ExternalTokenModel>))]
        public async Task<IActionResult> GetAllAsync()
        {
            IEnumerable<IExternalToken> tokens = await _externalTokenService.GetAllTokensAsync();

            var result = tokens.Select(token => Map(token));

            return Ok(new ListResult<ExternalTokenModel>() { Data = result });
        }

        [Route("{externalTokenAddress}")]
        [HttpGet]
        [Produces(typeof(ExternalTokenModel))]
        public async Task<IActionResult> GetByAddressAsync([FromRoute]string externalTokenAddress)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IExternalToken token = await _externalTokenService.GetByAddressAsync(externalTokenAddress);

            return Ok(Map(token));
        }

        /// <summary>
        /// May take significant time to complete.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Route("create")]
        [HttpPost]
        [Produces(typeof(ExternalTokenModel))]
        public async Task<IActionResult> CreateTransferContract([FromBody]CreateExternalTokenModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            BigInteger amount = BigInteger.Parse(model.InitialSupply);
            IExternalToken token = await _externalTokenService.CreateExternalTokenAsync(model.TokenName, 
                model.Divisibility, model.TokenSymbol, model.Version, model.AllowEmission, amount);

            return Ok(Map(token));
        }

        /// <summary>
        /// May take significant time to complete.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Route("issue")]
        [HttpPost]
        [Produces(typeof(ExternalTokenModel))]
        public async Task<IActionResult> IssueTokens([FromBody]IssueTokensModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            BigInteger amount = BigInteger.Parse(model.Amount);
            string trHash = await _externalTokenService.IssueTokensAsync(model.ExternalTokenAddress, model.ToAddress, amount);

            return Ok(new TransactionResponse
            {
                TransactionHash = trHash
            });
        }

        /// <summary>
        /// May take significant time to complete.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Route("balance/{externalTokenAddress}/{ownerAddress}")]
        [HttpGet]
        [Produces(typeof(BalanceModel))]
        public async Task<IActionResult> GetBalance([FromRoute]string externalTokenAddress, [FromRoute]string ownerAddress)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            BigInteger balance = await _externalTokenService.GetBalance(externalTokenAddress, ownerAddress);

            return Ok(new BalanceModel
            {
                Amount = balance.ToString()
            });
        }

        private ExternalTokenModel Map(IExternalToken token)
        {
            return new ExternalTokenModel
            {
                ContractAddress = token.ContractAddress,
                Id = token.Id,
                Name = token.Name,
                InitialSupply = token.InitialSupply,
                TokenSymbol = token.TokenSymbol,
                Version = token.Version,
                Divisibility = token.Divisibility,
            };
        }
    }
}
