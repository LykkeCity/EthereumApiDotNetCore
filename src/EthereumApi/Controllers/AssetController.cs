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
using System.Numerics;

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

        [HttpGet]
        [Produces(typeof(ListResult<CoinResult>))]
        public async Task<IActionResult> GetAllAdapters()
        {
            IEnumerable<ICoin> all = await _assetContractService.GetAll();
            IEnumerable<CoinResult> result = all.Select(x => new CoinResult()
            {
                AdapterAddress = x.AdapterAddress,
                Blockchain = x.Blockchain,
                BlockchainDepositEnabled = x.BlockchainDepositEnabled,
                ContainsEth = x.ContainsEth,
                ExternalTokenAddress = x.ExternalTokenAddress,
                Id = x.Id,
                Multiplier = x.Multiplier,
                Name = x.Name
            });

            return Ok(new ListResult<CoinResult>()
            {
                Data = result
            });
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

        [Route("balance/{coinAdapterAddress}/{userAddress}")]
        [HttpGet]
        [Produces(typeof(BalanceModel))]
        public async Task<IActionResult> CreateCoinAdapter([FromRoute]string coinAdapterAddress, [FromRoute]string userAddress)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            BigInteger amount = await _assetContractService.GetBalance(coinAdapterAddress, userAddress);

            return Ok(new BalanceModel
            {
                Amount = amount.ToString()
            });
        }
    }
}
