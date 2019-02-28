using System;
using System.Linq;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Web3;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.EthereumCore.Services.Signature
{
    public class NonceCalculator : INonceCalculator
    {
        private readonly IClient                _client;
        private readonly EthGetTransactionCount _getTransactionCount;

        public NonceCalculator(Web3 web3)
        {
            _getTransactionCount = new EthGetTransactionCount(web3.Client);
            _client              = web3.Client;
        }


        public async Task<HexBigInteger> GetNonceAsync(string fromAddress, bool checkTxPool)
        {
            if (checkTxPool)
            {
                var txPool   = await _client.SendRequestAsync<JValue>(new RpcRequest($"{Guid.NewGuid()}", "parity_nextNonce", fromAddress));

                if (txPool != null)
                {
                    var bigInt = new HexBigInteger(txPool.Value.ToString());
                    return bigInt;
                }
            }

            return await _getTransactionCount.SendRequestAsync(fromAddress, BlockParameter.CreatePending());
        }
    }
}