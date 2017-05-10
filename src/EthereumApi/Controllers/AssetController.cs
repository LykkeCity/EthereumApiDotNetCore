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
    [Route("api/coinAdapter")]
    [Produces("application/json")]
    public class СoinAdapterController : Controller
    {
        private readonly AssetContractService _assetContractService;
        private readonly ILog _logger;

        public СoinAdapterController(AssetContractService assetContractService, ILog logger)
        {
            _assetContractService = assetContractService;
            _logger = logger;
        }

        [Route("create")]
        [HttpPost]
        [Produces(typeof(RegisterResponse))]
        public async Task<IActionResult> CreateCoinAdapter([FromBody]CreateAssetModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ICoin asset = new Coin()
            {
                ExternalTokenAddress = model.ExternalTokenAddress,
                ContainsEth = model.ContainsEth,
                Blockchain = model.Blockchain,
                BlockchainDepositEnabled = true,
                Id = Guid.NewGuid().ToString(),
                Multiplier = model.Multiplier,
                Name = model.Name,
            };

            string contractAddress = await _assetContractService.CreateCoinContract(asset);

            return Ok(new RegisterResponse
            {
                Contract = contractAddress
            });
        }
    }
}
