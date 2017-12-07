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
using Core.Repositories;
using LkeServices.Signature;
using Nethereum.Web3;
using Nethereum.RPC.Eth.DTOs;
using Core.Settings;
using Services.Signature;

namespace Service.Tests.BugReproduction
{
    [TestClass]
    public class GasPriceTest : BaseTest
    {
        public LykkeSignedTransactionManager _transactionManager { get; private set; }

        [TestInitialize]
        public void Init()
        {
            var baseSettings = Config.Services.GetService<IBaseSettings>();
            var web3 = Config.Services.GetService<Web3>();
            var signatureApi = Config.Services.GetService<ILykkeSigningAPI>();
            var nonceCalculator = Config.Services.GetService<INonceCalculator>();
            var transactionRouter = Config.Services.GetService<ITransactionRouter>();
            var gasPriceRepository = Config.Services.GetService<IGasPriceRepository>();

            _transactionManager = new LykkeSignedTransactionManager(baseSettings, nonceCalculator, signatureApi, transactionRouter, web3, gasPriceRepository);
        }

        [TestMethod]
        public async Task Get_GasPriceTest()
        {
            string hash = await _transactionManager.
                SendTransactionAsync(new TransactionInput("sometext", new Nethereum.Hex.HexTypes.HexBigInteger(100), _clientA));
        }
    }
}
