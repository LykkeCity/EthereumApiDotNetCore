using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Services.Signature
{
    public class NonceCalculator : INonceCalculator
    {
        private readonly IClient _client;
        private readonly EthGetTransactionCount _getTransactionCount;

        public NonceCalculator(Web3 web3)
        {
            _getTransactionCount = new EthGetTransactionCount(web3.Client);
            _client = web3.Client;
        }


        public async Task<HexBigInteger> GetNonceAsync(string fromAddress, bool checkTxPool)
        {
            var txPool = await _client.SendRequestAsync<JValue>(new RpcRequest($"{Guid.NewGuid()}", "parity_nextNonce", fromAddress));

            var bigInt = new HexBigInteger(txPool.Value.ToString());
            return bigInt;
        }
    }
}