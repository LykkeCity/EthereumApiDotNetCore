using Core.Repositories;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Services.Signature
{
    public interface INonceCalculator
    {
        Task<HexBigInteger> GetNonceAsync(TransactionInput transaction);
        Task<HexBigInteger> GetNonceAsync(string fromAddress);
    }

    public class NonceCalculator : INonceCalculator
    {
        private readonly Web3 _web3;
        private readonly INonceRepository _nonceRepository;

        public NonceCalculator(Web3 web3, INonceRepository nonceRepository)
        {
            _web3 = web3;
            _nonceRepository = nonceRepository;
            _nonceRepository.CleanAsync().Wait();
        }

        public async Task<HexBigInteger> GetNonceAsync(TransactionInput transaction)
        {
            var nonce = await GetNonceAsync(transaction.From, transaction.Nonce);

            return nonce;
        }

        public async Task<HexBigInteger> GetNonceAsync(string fromAddress)
        {
            var nonce = await GetNonceAsync(fromAddress, null);

            return nonce;
        }

        private async Task<HexBigInteger> GetNonceAsync(string fromAddress, HexBigInteger nonce = null)
        {
            var ethGetTransactionCount = new EthGetTransactionCount(_web3.Client);
            
            if (nonce == null)
            {
                nonce = await ethGetTransactionCount.SendRequestAsync(fromAddress, BlockParameter.CreatePending()).ConfigureAwait(false);
            }

            return nonce;
        }
    }
}
