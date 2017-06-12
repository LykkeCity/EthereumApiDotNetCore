using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tests;
using Microsoft.Extensions.DependencyInjection;
using SigningServiceApiCaller;
using Services;
using System.Numerics;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Diagnostics;

namespace Service.Tests.BugReproduction
{
    [TestClass]
    public class WrongSignBug : BaseTest
    {
        private IHashCalculator _hashCalculator;

        private ILykkeSigningAPI _lykkeSigningAPI;

        [TestInitialize]
        public void Init()
        {
            _lykkeSigningAPI = Config.Services.GetService<ILykkeSigningAPI>();
            _hashCalculator = Config.Services.GetService<IHashCalculator>();
        }

        [TestMethod]
        public async Task BugReproduction()
        {
            var id = Guid.NewGuid();
            var from = _clientA;
            var to = _clientB;
            var adapter = _tokenAdapterAddress;

            BigInteger limit = new BigInteger(1000000000000000000);//1ETH
            for (BigInteger amount = 0; amount < limit; amount++)
            {
                string hash = _hashCalculator.GetHash(id, adapter, from, to, amount).ToHex();
                Trace.TraceInformation($"{hash.Length}");
                var response = await _lykkeSigningAPI.ApiEthereumSignHashPostAsync(new SigningServiceApiCaller.Models.EthereumHashSignRequest()
                {
                    FromProperty = from,
                    Hash = hash
                });

                if (response.SignedHash.Length != 130)
                {
                    Trace.TraceInformation($"{id} - {adapter} - {from} - {to} - {amount} - {response.SignedHash}");
                    Assert.Fail();
                }
            }
        }
    }
}
