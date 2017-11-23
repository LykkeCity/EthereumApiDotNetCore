﻿using System.Threading.Tasks;
using EthereumApiSelfHosted.Models;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace EthereumApi.Controllers
{
    [Route("api/erc20deposits")]
    [Produces("application/json")]
    public class Erc20DepositContractsController : Controller
    {
        private readonly IErc20DepositContractService _contractService;

        public Erc20DepositContractsController(IErc20DepositContractService contractService)
        {
            _contractService = contractService;
        }


        [HttpPost]
        [ProducesResponseType(typeof(RegisterResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> CreateDepositContract([FromQuery] string userAddress)
        {
            var contractAddress = await _contractService.AssignContract(userAddress);

            return Ok(new RegisterResponse
            {
                Contract = contractAddress
            });
        }

        [HttpGet]
        [ProducesResponseType(typeof(RegisterResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> GetDepositContract([FromQuery] string userAddress)
        {
            var contractAddress = await _contractService.GetContractAddress(userAddress);

            return Ok(new RegisterResponse
            {
                Contract = contractAddress
            });
        }
    }
}