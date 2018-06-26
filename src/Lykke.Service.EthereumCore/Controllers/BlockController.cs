using Lykke.Service.EthereumCore.BusinessModels;
using Lykke.Service.EthereumCore.BusinessModels.Erc20;
using Lykke.Service.EthereumCore.BusinessModels.PrivateWallet;
using Common.Log;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Models;
using Lykke.Service.EthereumCore.Models.Models;
using Lykke.Service.EthereumCore.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Lykke.Service.EthereumCore.Services.Erc20;
using Lykke.Service.EthereumCore.Services.PrivateWallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore;
using Lykke.Service.EthereumCore.Core.Services;

namespace Lykke.Service.EthereumCore.Controllers
{
    [Route("api/block")]
    [Produces("application/json")]
    public class BlockController : Controller
    {
        private readonly IWeb3 _web3;
        private readonly ILog _log;

        public BlockController(IWeb3 web3, ILog log)
        {
            _web3 = web3;
            _log = log;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CurrentBlockModel), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetCurrentBlockAsync()
        {
            if (!ModelState.IsValid)
            {
                throw new ClientSideException(ExceptionType.WrongParams, JsonConvert.SerializeObject(ModelState.Errors()));
            }

             var blockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();

            return Ok(new CurrentBlockModel()
            {
                LatestBlockNumber = (ulong)blockNumber.Value
            });
        }
    }
}
