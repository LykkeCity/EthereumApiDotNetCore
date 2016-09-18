using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Core.ContractEvents;
using Core.Settings;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace Services
{
	public interface IContractService
	{
		Task<string> GenerateMainContract();
		Task<string> GenerateUserContract();
		Task<HexBigInteger> CreateFilterEventForUserContractPayment();
		Task<UserPaymentEvent[]> GetNewPaymentEvents(HexBigInteger filter);
		Task<string[]> GenerateUserContracts(int count = 10);
		Task<BigInteger> GetCurrentBlock();
	}

	public class ContractService : IContractService
	{
		private readonly IBaseSettings _settings;

		public ContractService(IBaseSettings settings)
		{
			_settings = settings;
		}

		public async Task<string> GenerateMainContract()
		{
			var web3 = new Web3(_settings.EthereumUrl);

			// unlock account for 120 seconds
			await web3.Personal.UnlockAccount.SendRequestAsync(_settings.EthereumMainAccount, _settings.EthereumMainAccountPassword, new HexBigInteger(120));

			// deploy contract
			var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(_settings.MainContract.Abi, _settings.MainContract.ByteCode, _settings.EthereumMainAccount, new HexBigInteger(500000));

			// get contract transaction
			TransactionReceipt receipt;
			while ((receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash)) == null)
			{
				await Task.Delay(100);
			}

			// check if contract byte code is deployed
			var code = await web3.Eth.GetCode.SendRequestAsync(receipt.ContractAddress);

			if (string.IsNullOrWhiteSpace(code) || code == "0x")
			{
				throw new Exception("Code was not deployed correctly, verify bytecode or enough gas was to deploy the contract");
			}

			return receipt.ContractAddress;
		}


		public async Task<string> GenerateUserContract()
		{
			var web3 = new Web3(_settings.EthereumUrl);

			// unlock account for 120 seconds
			await web3.Personal.UnlockAccount.SendRequestAsync(_settings.EthereumMainAccount, _settings.EthereumMainAccountPassword, new HexBigInteger(120));

			// deploy contract (pass mainContractAddress to contract contructor)
			var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(_settings.UserContract.Abi, _settings.UserContract.ByteCode, _settings.EthereumMainAccount, new HexBigInteger(500000), _settings.EthereumMainContractAddress);

			// get contract transaction
			TransactionReceipt receipt;
			while ((receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash)) == null)
			{
				await Task.Delay(100);
			}

			// check if contract byte code is deployed
			var code = await web3.Eth.GetCode.SendRequestAsync(receipt.ContractAddress);

			if (string.IsNullOrWhiteSpace(code) || code == "0x")
			{
				throw new Exception("Code was not deployed correctly, verify bytecode or enough gas was to deploy the contract");
			}

			return receipt.ContractAddress;
		}

		public async Task<HexBigInteger> CreateFilterEventForUserContractPayment()
		{
			var contract = new Web3(_settings.EthereumUrl).Eth.GetContract(_settings.MainContract.Abi, _settings.EthereumMainContractAddress);

			return await contract.CreateFilterAsync();
		}

		public async Task<UserPaymentEvent[]> GetNewPaymentEvents(HexBigInteger filter)
		{
			var web3 = new Web3(_settings.EthereumUrl);

			var contract = web3.Eth.GetContract(_settings.MainContract.Abi, _settings.EthereumMainContractAddress);
			var ev = contract.GetEvent("PaymentFromUser");

			var logs = await ev.GetFilterChanges<UserPaymentEvent>(filter);

			// group by because of block chain reconstructions
			return logs.GroupBy(x => new { x.Log.Address, x.Log.Data })
						.Select(x => x.First().Event)
						.ToArray();
		}

		public async Task<string[]> GenerateUserContracts(int count = 10)
		{
			var web3 = new Web3(_settings.EthereumUrl);

			// unlock account for 120 seconds
			await web3.Personal.UnlockAccount.SendRequestAsync(_settings.EthereumMainAccount, _settings.EthereumMainAccountPassword, new HexBigInteger(120));

			var transactionHashList = new List<string>();

			// sends <count> contracts
			for (var i = 0; i < count; i++)
			{
				// deploy contract (pass mainContractAddress to contract contructor)
				var transactionHash =
					await
						web3.Eth.DeployContract.SendRequestAsync(_settings.UserContract.Abi, _settings.UserContract.ByteCode,
							_settings.EthereumMainAccount, new HexBigInteger(500000), _settings.EthereumMainContractAddress);

				transactionHashList.Add(transactionHash);
			}

			// wait for all <count> contracts transactions
			var contractList = new List<string>();
			for (var i = 0; i < count; i++)
			{
				// get contract transaction
				TransactionReceipt receipt;
				while ((receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHashList[i])) == null)
				{
					await Task.Delay(100);
				}

				// check if contract byte code is deployed
				var code = await web3.Eth.GetCode.SendRequestAsync(receipt.ContractAddress);

				if (string.IsNullOrWhiteSpace(code) || code == "0x")
				{
					throw new Exception("Code was not deployed correctly, verify bytecode or enough gas was to deploy the contract");
				}

				contractList.Add(receipt.ContractAddress);
			}

			return contractList.ToArray();
		}

		public async Task<BigInteger> GetCurrentBlock()
		{
			var web3 = new Web3(_settings.EthereumUrl);
			var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
			return blockNumber.Value;
		}
	}
}
