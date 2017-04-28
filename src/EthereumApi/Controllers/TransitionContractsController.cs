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
    [Route("api/asset")]
    [Produces("application/json")]
    public class TransitionContractsController : Controller
    {
        private readonly ILog _logger;
        private readonly TransferContractService _transferContractService;

        public TransitionContractsController(TransferContractService transferContractService, ILog logger)
        {
            _transferContractService = transferContractService;
            _logger = logger;
        }

        [Route("create")]
        [HttpPost]
        [Produces(typeof(TransactionResponse))]
        public async Task<IActionResult> CreateTransferContract([FromBody]CreateTransitionContractModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            //await Log("Transition", "Create", model);

            string contractAddress = await _transferContractService.CreateTransferContract(model.UserAddress, 
                model.CoinAdapterAddress,
                model.ExternalTokenAddress, model.IsAdapterWithEthereum);

            return Ok(new RegisterResponse
            {
                Contract = contractAddress
            });
        }

        [Route("contractAddress/{userAddress}/{coinAdapterAddress}")]
        [HttpGet]
        [Produces(typeof(TransactionResponse))]
        public async Task<IActionResult>  GetAddress(string userAddress, string coinAdapterAddress)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ITransferContract contract = await _transferContractService.GetTransferContract(userAddress,
                coinAdapterAddress);

            if (contract == null)
            {
                return NotFound();
            }

            return Ok(new RegisterResponse
            {
                Contract = contract.ContractAddress
            });
        }
    }
}
