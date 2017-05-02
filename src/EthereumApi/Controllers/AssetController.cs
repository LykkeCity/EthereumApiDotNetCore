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
    [Route("api/asset")]
    [Produces("application/json")]
    public class AssetController : Controller
    {
        private readonly AssetContractService _assetContractService;
        private readonly ILog _logger;

        public AssetController(AssetContractService assetContractService, ILog logger)
        {
            _assetContractService = assetContractService;
            _logger = logger;
        }

        [Route("create")]
        [HttpPost]
        [Produces(typeof(TransactionResponse))]
        public async Task<IActionResult> CreateTransferContract([FromBody]CreateAssetModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ICoin asset = new Coin()
            {
                Blockchain = model.Blockchain,
                BlockchainDepositEnabled = true,
                Id = "1",
                Multiplier = model.Multiplier,
                Name = model.Name,
            };

            INewEthereumContract ethereumContract = new EthereumContract()
            {
                Abi = model.Abi,
                ByteCode = model.Bytecode
            };

            string contractAddress = await _assetContractService.CreateCoinContract(asset, ethereumContract);

            return Ok(new RegisterResponse
            {
                Contract = contractAddress
            });
        }
    }
}
