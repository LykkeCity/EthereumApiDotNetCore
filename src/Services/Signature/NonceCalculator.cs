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
                var txPool   = await _client.SendRequestAsync<JObject>(new RpcRequest($"{Guid.NewGuid()}", "txpool_inspect"));
                var maxNonce = txPool["pending"]
                    .Cast<JProperty>()
                    .FirstOrDefault(x => x.Name.Equals(fromAddress, StringComparison.OrdinalIgnoreCase))?
                    .FirstOrDefault()?
                    .Cast<JProperty>()
                    .Select(x => long.Parse(x.Name))
                    .Max();

                if (maxNonce.HasValue)
                {
                    return new HexBigInteger(new BigInteger(maxNonce.Value + 1));
                }
            }

            return await _getTransactionCount.SendRequestAsync(fromAddress, BlockParameter.CreatePending());
        }
    }
}