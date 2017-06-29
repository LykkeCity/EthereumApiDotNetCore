using BusinessModels;
using Core.Exceptions;
using EthereumSamuraiApiCaller;
using EthereumSamuraiApiCaller.Models;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Util;
using Nethereum.Web3;
using Services.Signature;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Services.PrivateWallet
{
    public interface IEthereumIndexerService
    {
        Task<IEnumerable<TransactionModel>> GetTransactionHistory(AddressTransactions addressTransactions);
        Task<BigInteger> GetEthBalance(string address);
    }

    public class EthereumIndexerService : IEthereumIndexerService
    {
        private readonly Web3 _web3;
        private AddressUtil _addressUtil;
        private IEthereumSamuraiApi _ethereumSamuraiApi;

        public EthereumIndexerService(Web3 web3, IEthereumSamuraiApi ethereumSamuraiApi)
        {
            _addressUtil = new AddressUtil();
            _ethereumSamuraiApi = ethereumSamuraiApi;
            _web3 = web3;
        }

        public async Task<BigInteger> GetEthBalance(string address)
        {
            var response = await _ethereumSamuraiApi.ApiBalanceGetBalanceByAddressGetAsync(address);
            var balanceResponse = response as BalanceResponse;
            ThrowOnError(response);
            BigInteger balance = BigInteger.Parse(balanceResponse.Amount);

            return balance;
        }

        public async Task<IEnumerable<TransactionModel>> GetTransactionHistory(AddressTransactions addressTransactions)
        {
           var response = await _ethereumSamuraiApi.ApiTransactionByAddressGetAsync(addressTransactions.Address, addressTransactions.Start, addressTransactions.Count);
            var transactionResponse = response as FilteredTransactionsResponse;
            ThrowOnError(transactionResponse);
            List<TransactionModel> result = new List<TransactionModel>(transactionResponse.Transactions?.Count ?? 0);

            foreach (var transaction in transactionResponse.Transactions)
            {
                result.Add(new TransactionModel()
                {
                    BlockHash = transaction.BlockHash,
                    BlockNumber = transaction.BlockNumber.Value,
                    BlockTimestamp = transaction.BlockTimestamp.Value,
                    ContractAddress= transaction.ContractAddress,
                    FromProperty= transaction.FromProperty,
                    Gas = transaction.Gas,
                    GasPrice= transaction.GasPrice,
                    GasUsed= transaction.GasUsed,
                    Input= transaction.Input,
                    Nonce= transaction.Nonce,
                    To= transaction.To,
                    TransactionHash= transaction.TransactionHash,
                    TransactionIndex= transaction.TransactionIndex.Value,
                    Value = transaction.Value
                });
            }

            return result;
        }

        private void ThrowOnError(object transactionResponse)
        {
            if (transactionResponse == null)
            {
                var exception = transactionResponse as ApiException;
                var errorMessage = exception?.Error?.Message ?? "Response is empty";

                throw new Exception(errorMessage);
            }
        }
    }
}
