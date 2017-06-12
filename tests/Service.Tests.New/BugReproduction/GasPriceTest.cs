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
using LkeServices.Signature;
using Nethereum.Web3;
using Nethereum.RPC.Eth.DTOs;

namespace Service.Tests.BugReproduction
{
    [TestClass]
    public class GasPriceTest : BaseTest
    {
        public LykkeSignedTransactionManager _transactionManager { get; private set; }

        [TestInitialize]
        public void Init()
        {
            var web3 = Config.Services.GetService<Web3>();
            var signatureApi = Config.Services.GetService<ILykkeSigningAPI>();
            _transactionManager = new LykkeSignedTransactionManager(web3, signatureApi);
        }

        [TestMethod]
        public async Task Get_GasPriceTest()
        {
            string hash = await _transactionManager.
                SendTransactionAsync(new TransactionInput("sometext", new Nethereum.Hex.HexTypes.HexBigInteger(100), _clientA));
        }
    }
}
