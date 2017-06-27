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
using System.Text;
using System.Threading.Tasks;

namespace Services.PrivateWallet
{
    public interface IEthereumIndexerService
    {
        Task<IEnumerable<TransactionModel>> GetTransactionHistory(AddressTransactions addressTransactions);
    }

    public class EthereumIndexerService : IEthereumIndexerService
    {
        private readonly Web3 _web3;
        private readonly INonceCalculator _nonceCalculator;
        private AddressUtil _addressUtil;
        private IEthereumSamuraiApi _ethereumSamuraiApi;

        public EthereumIndexerService(Web3 web3, IEthereumSamuraiApi ethereumSamuraiApi)
        {
            _addressUtil = new AddressUtil();
            _ethereumSamuraiApi = ethereumSamuraiApi;
            _web3 = web3;
        }

        public async Task<IEnumerable<TransactionModel>> GetTransactionHistory(AddressTransactions addressTransactions)
        {
           var response = await _ethereumSamuraiApi.ApiTransactionByAddressGetAsync(addressTransactions.Address, addressTransactions.Start, addressTransactions.Count);
            var transactionResponse = response as FilteredTransactionsResponse;
            if (transactionResponse == null)
            {
                var exception = response as ApiException;
                var errorMessage = exception?.Error?.Message ?? "Response is empty";

                throw new Exception(errorMessage);
            }

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
    }
}
