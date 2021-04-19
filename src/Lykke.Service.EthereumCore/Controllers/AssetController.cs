using System;
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
using Nethereum.Util;

namespace Lykke.Service.EthereumCore.Controllers
{
    //ForAdminOnly
    [Route("api/coinAdapter")]
    [Produces("application/json")]
    public class СoinAdapterController : Controller
    {
        private readonly AssetContractService _assetContractService;
        private readonly ILog _logger;
        private readonly AddressUtil _addressUtil;

        public СoinAdapterController(AssetContractService assetContractService, ILog logger)
        {
            _addressUtil = new AddressUtil();
            _assetContractService = assetContractService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ListResult<CoinResult>), 200)]
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

        //method was created for integration convinience
        [HttpGet("exists/{adapterAddress}")]
        [ProducesResponseType(typeof(ExistsModel), 200)]
        public async Task<IActionResult> IsValidAddress(string adapterAddress)
        {
            var coin = await _assetContractService.GetByAddress(adapterAddress);
            return Ok(new ExistsModel()
            {
                Exists = coin != null
            });
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CoinResult), 200)]
        [ProducesResponseType(typeof(void), 404)]
        public async Task<IActionResult> GetAdapter(string id)
        {
            return await GetCoinAdapter(id, _assetContractService.GetById);
        }

        [HttpGet("address/{adapterAddress}")]
        [ProducesResponseType(typeof(CoinResult), 200)]
        [ProducesResponseType(typeof(void), 404)]
        public async Task<IActionResult> GetAdapterByAddress(string adapterAddress)
        {
            return await GetCoinAdapter(adapterAddress, _assetContractService.GetByAddress);
        }

        [Route("create")]
        [HttpPost]
        [ProducesResponseType(typeof(RegisterResponse), 200)]
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
        [ProducesResponseType(typeof(BalanceModel), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> CreateCoinAdapter([FromRoute]string coinAdapterAddress, [FromRoute]string userAddress)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            BigInteger amount = await _assetContractService.GetBalance(coinAdapterAddress, _addressUtil.ConvertToChecksumAddress(userAddress));

            return Ok(new BalanceModel
            {
                Amount = amount.ToString()
            });
        }

        private async Task<IActionResult> GetCoinAdapter(string argument, Func<string, Task<ICoin>> recieveFunc)
        {
            if (string.IsNullOrEmpty(argument))
            {
                return BadRequest("identifier is missing");
            }

            ICoin coinAdapter = await recieveFunc(argument);//(id);

            if (coinAdapter == null)
            {
                return NotFound();
            }

            var result = new CoinResult()
            {
                AdapterAddress = coinAdapter.AdapterAddress,
                Blockchain = coinAdapter.Blockchain,
                BlockchainDepositEnabled = coinAdapter.BlockchainDepositEnabled,
                ContainsEth = coinAdapter.ContainsEth,
                ExternalTokenAddress = coinAdapter.ExternalTokenAddress,
                Id = coinAdapter.Id,
                Multiplier = coinAdapter.Multiplier,
                Name = coinAdapter.Name
            };

            return Ok(result);
        }
    }
}
