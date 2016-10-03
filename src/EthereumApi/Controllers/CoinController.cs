using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Exceptions;
using EthereumApi.Models;
using Microsoft.AspNetCore.Mvc;
using Services;
using Services.Coins;

namespace EthereumApi.Controllers
{
	[Route("api/coin")]
	[Produces("application/json")]
	public class CoinController : Controller
	{
		private readonly ICoinContractService _coinContractService;

		public CoinController(ICoinContractService coinContractService)
		{
			_coinContractService = coinContractService;
		}
		
		[Route("swap")]
		[HttpPost]
		[Produces(typeof(TransactionResponse))]
		public async Task<IActionResult> Swap(SwapModel model)
		{
			if (ModelState.IsValid)
				throw new BackendException(BackendExceptionType.MissingRequiredParams);
			var transaction = await _coinContractService.Swap(model.Id, model.ClientA, model.ClientB, model.CoinA, model.CoinB,
				model.AmountA, model.AmountB, model.SignA, model.SignB);
			return Ok(new TransactionResponse { TransactionHash = transaction });
		}

		[Route("cashout")]
		[HttpPost]
		[Produces(typeof(TransactionResponse))]
		public async Task<IActionResult> Cashout(CashoutModel model)
		{
			if (ModelState.IsValid)
				throw new BackendException(BackendExceptionType.MissingRequiredParams);
			var transaction = await _coinContractService.CashOut(model.Id, model.Coin, model.Client, model.To, model.Amount, model.Sign);
			return Ok(new TransactionResponse { TransactionHash = transaction });
		}

		[Route("cashin")]
		[HttpPost]
		[Produces(typeof(TransactionResponse))]
		public async Task<IActionResult> Cashin(CashInModel model)
		{
			if (ModelState.IsValid)
				throw new BackendException(BackendExceptionType.MissingRequiredParams);
			var transaction = await _coinContractService.CashIn(model.Coin, model.Receiver, model.Amount);
			return Ok(new TransactionResponse { TransactionHash = transaction });
		}
	}
}
