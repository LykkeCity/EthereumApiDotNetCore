using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Core;
using Core.Repositories;
using Core.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.ABI.Encoders;
using Nethereum.ABI.Util;
using Services;
using Services.Coins;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;
using Core.Utils;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;
using Services.Coins.Models;
using AzureStorage.Queue;
using Nethereum.Util;
using Nethereum.Signer;
using System.Diagnostics;

namespace Tests
{
    //Todo: put tests on separate tables
    //Warning: tests consumes ethereum on mainAccount. Run on testnet only!
    [TestClass]
    public class ExchangeServiceTests : BaseTest
    {
        public static string _ethereumAdapterAddress = "0x1c4ca817d1c61f9c47ce2bec9d7106393ff981ce";
        public static string _clientEthereumTransferAddress = "";

        //BCAPTokenAddress -         0xce2ef46ecc168226f33b6f6b8a56e90450d0d2c0
        //BCAPTokenAdapter-	   0x1e8e8ccbd9a7a8d82875054aa8342159d96356a9
        //BCAPTransferAddress -      0x7ff01d3225726eb3dd3356fc57e71e5ec0aab042
        private IBaseSettings _settings;
        private ICoinRepository _coinRepository;
        private IExchangeContractService _exchangeService;
        private IEthereumTransactionService _transactionService;
        private IErcInterfaceService _ercService;
        private ITransferContractService _transferContractService;
        private IPaymentService _paymentService;

        [TestInitialize]
        public void Init()
        {
            _settings = Config.Services.GetService<IBaseSettings>();
            _coinRepository = Config.Services.GetService<ICoinRepository>();
            _exchangeService = Config.Services.GetService<IExchangeContractService>();
            _transactionService = Config.Services.GetService<IEthereumTransactionService>();
            _ercService = Config.Services.GetService<IErcInterfaceService>();
            _transferContractService = Config.Services.GetService<ITransferContractService>();
            _paymentService = Config.Services.GetService<IPaymentService>();
        }

        #region TokenAdapter

        [TestMethod]
        public async Task TestCashinTokenFlow()
        {
            //Transfer to transition contract
            ICoin colorCoin = await _coinRepository.GetCoinByAddress(_tokenAdapterAddress);
            BigInteger cashinAmount = new BigInteger(100);

            await CashinTokens(_externalTokenAddress, _clientTokenTransferAddress, cashinAmount, _tokenAdapterAddress, _clientA);
            string transferUser = await _transferContractService.GetTransferAddressUser(colorCoin.AdapterAddress, _clientTokenTransferAddress);
            var currentBalance = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, _clientA);

            Assert.AreEqual(_clientA.ToLower(), transferUser);
            Assert.IsTrue(currentBalance >= cashinAmount);
        }

        [TestMethod]
        public async Task TestTransferTokens()
        {
            var colorCoin = await _coinRepository.GetCoinByAddress(_tokenAdapterAddress);
            var toAddress = _settings.EthereumMainAccount;
            await CashinTokens(_externalTokenAddress, _clientTokenTransferAddress, new BigInteger(100), _tokenAdapterAddress, _clientA);
            var transferUser = await _transferContractService.GetTransferAddressUser(colorCoin.AdapterAddress, _clientTokenTransferAddress);
            var currentBalance = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, _clientA);

            Assert.AreEqual(transferUser, _clientA.ToLower());

            var guid = Guid.NewGuid();
            EthUtils.GuidToBigInteger(guid);
            var externalSign = await _exchangeService.GetSign(guid, _tokenAdapterAddress, _clientA, toAddress, currentBalance);
            var transferHash = await _exchangeService.Transfer(guid, _tokenAdapterAddress, _clientA, toAddress,
                currentBalance, externalSign);

            while (await _transactionService.GetTransactionReceipt(transferHash) == null)
                await Task.Delay(100);

            var currentBalanceOnAdapter = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, _clientA);
            var newBalance = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, toAddress);

            Assert.IsTrue(await _transactionService.IsTransactionExecuted(transferHash, Constants.GasForCoinTransaction));
            Assert.IsTrue(currentBalanceOnAdapter == 0);
            Assert.IsTrue(currentBalance <= newBalance);
        }

        [TestMethod]
        public async Task TestTransferWithChangeTokens()
        {
            var colorCoin = await _coinRepository.GetCoinByAddress(_tokenAdapterAddress);
            var toAddress = _ethereumCoinOwnerB;
            await CashinTokens(_externalTokenAddress, _clientTokenTransferAddress, new BigInteger(100), _tokenAdapterAddress, _clientA);
            var transferUser = await _transferContractService.GetTransferAddressUser(colorCoin.AdapterAddress, _clientTokenTransferAddress);
            var currentBalance = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, _clientA);
            var change = new BigInteger(50);

            Assert.AreEqual(transferUser, _clientA.ToLower());

            var guid = Guid.NewGuid();
            var externalFromSign = await _exchangeService.GetSign(guid, _tokenAdapterAddress, _clientA, toAddress, currentBalance);
            var externalToSign = await _exchangeService.GetSign(guid, _tokenAdapterAddress, toAddress, _clientA, change);
            var transferHash = await _exchangeService.TransferWithChange(guid, colorCoin.AdapterAddress, _clientA, toAddress,
                currentBalance, externalFromSign, change, externalToSign);

            while (await _transactionService.GetTransactionReceipt(transferHash) == null)
                await Task.Delay(100);

            var currentBalanceOnAdapter = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, _clientA);
            var newBalance = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, toAddress);

            Assert.IsTrue(await _transactionService.IsTransactionExecuted(transferHash, Constants.GasForCoinTransaction));
            Assert.IsTrue(currentBalanceOnAdapter == change);
            Assert.IsTrue(currentBalanceOnAdapter <= newBalance);
        }

        private static string GetHash(Guid guid, string coinAdapterAddress, string fromAddress, string toAddress, BigInteger currentBalance)
        {
            return EthUtils.GuidToByteArray(guid).ToHex() +
                                        coinAdapterAddress.HexToByteArray().ToHex() +
                                        fromAddress.HexToByteArray().ToHex() +
                                        toAddress.HexToByteArray().ToHex() +
                                        EthUtils.BigIntToArrayWithPadding(currentBalance).ToHex();
        }

        [TestMethod]
        public async Task TestCashoutTokens()
        {
            var colorCoin = await _coinRepository.GetCoinByAddress(_tokenAdapterAddress);
            await CashinTokens(_externalTokenAddress, _clientTokenTransferAddress, new BigInteger(100), _tokenAdapterAddress, _clientA);
            var transferUser = await _transferContractService.GetTransferAddressUser(colorCoin.AdapterAddress, _clientTokenTransferAddress);
            var oldBalance = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, _clientA);

            Assert.AreEqual(_clientA.ToLower(), transferUser);

            var guid = Guid.NewGuid();
            var externalSign = await _exchangeService.GetSign(guid, _tokenAdapterAddress, _clientA, _clientA, oldBalance);
            var cashout = await _exchangeService.CashOut(guid, colorCoin.AdapterAddress, _clientA, _clientA,
                oldBalance, externalSign);

            while (await _transactionService.GetTransactionReceipt(cashout) == null)
                await Task.Delay(100);

            Assert.IsTrue(await _transactionService.IsTransactionExecuted(cashout, Constants.GasForCoinTransaction));
            var currentBalance = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, _clientA);
            var newBalance = await _ercService.GetBalanceForExternalTokenAsync(_clientA, _externalTokenAddress);

            Assert.IsTrue(oldBalance <= newBalance);
            Assert.IsTrue(currentBalance == 0);
        }

        [TestMethod]
        public async Task TestTransferTokens_WithRoundRobin()
        {
            Func<Task> testFunc = async () =>
                {
                    var colorCoin = await _coinRepository.GetCoinByAddress(_tokenAdapterAddress);
                    var toAddress = _settings.EthereumMainAccount;
                    await CashinTokens(_externalTokenAddress, _clientTokenTransferAddress, new BigInteger(100), _tokenAdapterAddress, _clientA);
                    var transferUser = await _transferContractService.GetTransferAddressUser(colorCoin.AdapterAddress, _clientTokenTransferAddress);
                    var currentBalance = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, _clientA);

                    Assert.AreEqual(transferUser, _clientA.ToLower());
                    int threadAmount = 4;
                    List<Task<string>> tasks = new List<Task<string>>(threadAmount);
                    for (int i = 0; i < threadAmount; i++)
                    {
                        tasks.Add(Task<string>.Run(async () =>
                        {
                            var guid = Guid.NewGuid();
                            var externalSign = await _exchangeService.GetSign(guid, _tokenAdapterAddress, _clientA, toAddress, currentBalance / threadAmount);
                            var trHash = await _exchangeService.Transfer(guid, _tokenAdapterAddress, _clientA, toAddress,
                                currentBalance / threadAmount, externalSign);

                            return trHash;
                        }));
                    }

                    await Task.WhenAll(tasks);
                    foreach (var task in tasks)
                    {
                        var trHash = task.Result;
                        while (await _transactionService.GetTransactionReceipt(trHash) == null)
                            await Task.Delay(100);
                    }

                    var currentBalanceOnAdapter = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, _clientA);
                    var newBalance = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, toAddress);

                    //Assert.IsTrue(await _transactionService.IsTransactionExecuted(transferHash, Constants.GasForCoinTransaction));
                    Assert.IsTrue(currentBalanceOnAdapter == 0);
                    Assert.IsTrue(currentBalance <= newBalance);
                };

            await testFunc();
            await Task.Delay(5 * 60 * 1000 + 10); //5min
            await testFunc();
        }

        #endregion

        #region EthereumAdapter


        //[TestMethod]
        //public async Task TestCashinEthereumFlow()
        //{
        //    //Transfer to transition contract
        //    ICoin colorCoin = await _coinRepository.GetCoinByAddress(_tokenAddress);
        //    BigInteger cashinAmount = new BigInteger(100);

        //    string result = paymentService.SendEthereum(settings.EthereumMainAccount,
        //        "0xbb0a9c08030898cdaf1f28633f0d3c8556155482", new System.Numerics.BigInteger(5000000000000000)).Result;

        //    await CashinTokens(_externalTokenAddress, _clientTokenTransferAddress, cashinAmount, _tokenAddress, ClientA);
        //    string transferUser = await _transferContractService.GetTransferAddressUser(colorCoin.AdapterAddress, _clientTokenTransferAddress);
        //    var currentBalance = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, ClientA);

        //    Assert.AreEqual(ClientA.ToLower(), transferUser);
        //    Assert.IsTrue(currentBalance >= cashinAmount);
        //}

        //[TestMethod]
        //public async Task TestCashoutEthereum()
        //{
        //    //TODO: complete
        //}

        #endregion

        #region Common

        [TestMethod]
        public async Task SignOld()
        {
            string hex = "";
            string privateKey = "";

            var secret = new EthECKey(privateKey); ;
            var hash = hex.HexToByteArray();
            var signature = secret.SignAndCalculateV(hash);
            string r = signature.R.Length == 32 ? signature.R.ToHex() : "00" + signature.R.ToHex();
            string s = signature.S.Length == 32 ? signature.S.ToHex() : "00" + signature.S.ToHex();
            string v = new[] { signature.V }.ToHex();

            var arrHex = (r + s + v);

            Trace.TraceInformation(arrHex);
        }


        [TestMethod]
        public async Task Test_EstimateCashoutGas()
        {
            var guid = Guid.NewGuid();
            var amount = new BigInteger(500000000000000000);
            var from = _clientA;
            string transferUser = "0x6303F9f7f1C57D0fF48fE6baD5161967f58de8fa";//"0x0f0b0affc64dc8d644ac45152c82f993dbb2931d";//0x6303F9f7f1C57D0fF48fE6baD5161967f58de8fa
            var to = "0x6303F9f7f1C57D0fF48fE6baD5161967f58de8fa";//_clientB;//"0xa5d3FEd752b8Fd22C3912290b82C8A6C25404c3A";//_clientB;//"0xfBfA258B9028c7d4fc52cE28031469214D10DAEB";

            var result = await _exchangeService.EstimateCashoutGas(guid, _ethereumAdapterAddress, from, to, amount, "");
            //var transactionHash = _exchangeService.CashOut(guid, _ethereumAdapterAddress, from, to, amount, "");
            //var resultS = transactionHash.Result;
            Assert.IsTrue(result.IsAllowed);
        }

        [TestMethod]
        public async Task TestCheckId_IsNotInList()
        {
            var guid = Guid.NewGuid();
            var result = await _exchangeService.CheckId(guid);

            Assert.IsTrue(result.IsFree);
        }

        [TestMethod]
        public async Task TestCheckSign_IsCorrect()
        {
            var guid = Guid.NewGuid();
            var amount = 50;
            EthUtils.GuidToBigInteger(guid);
            var strForHash = EthUtils.GuidToByteArray(guid).ToHex() +
                            _tokenAdapterAddress.HexToByteArray().ToHex() +
                            _clientA.HexToByteArray().ToHex() +
                            _clientA.HexToByteArray().ToHex() +
                            EthUtils.BigIntToArrayWithPadding(new BigInteger(amount)).ToHex();

            var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());
            var sign = Sign(hash, _privateKeyA).ToHex();
            //var externalSign = await _exchangeService.GetSign(guid, _tokenAdapterAddress, ClientA, ClientA, new BigInteger(amount));
            var result = _exchangeService.CheckSign(guid, _tokenAdapterAddress, _clientA, _clientA, new BigInteger(amount), sign);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestCheckSign_IsWrong()
        {
            var guid = Guid.NewGuid();
            var amount = 50;
            EthUtils.GuidToBigInteger(guid);
            var strForHash = EthUtils.GuidToByteArray(guid).ToHex() +
                            _ethereumAdapterAddress.HexToByteArray().ToHex() +
                            _clientA.HexToByteArray().ToHex() +
                            _clientA.HexToByteArray().ToHex() +
                            EthUtils.BigIntToArrayWithPadding(new BigInteger(amount)).ToHex();

            var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());
            var sign = Sign(hash, _privateKeyA).ToHex();
            var result = _exchangeService.CheckSign(guid, _ethereumAdapterAddress, _clientA, _clientA, new BigInteger(amount - 1), sign);

            Assert.IsFalse(result);
        }

        #endregion

        [TestMethod]
        public async Task SendTokensAsync()
        {
            var transferHash = await _ercService.Transfer(_externalTokenAddress, _settings.EthereumMainAccount, "0x46Ea3e8d85A06cBBd8c6a491a09409f5B59BEa28", 20000);
        }

        private byte[] Sign(byte[] hash, string privateKey)
        {
            var key = new EthECKey(privateKey.HexToByteArray(), true);
            EthECDSASignature signature = key.SignAndCalculateV(hash);
            //ToByteArrayUnsigned
            var r = signature.R.ToHex();
            var s = signature.S.ToHex();
            var v = new[] { signature.V }.ToHex();

            var arr = (r + s + v).HexToByteArray();
            return arr;
        }

        private async Task CashinTokens(string externalTokenAddress, string transferAddress,
            BigInteger amount, string coinAdapterAddress, string clientAddress)
        {
            var transferHash = await _ercService.Transfer(externalTokenAddress, _settings.EthereumMainAccount, transferAddress, amount);
            while (await _transactionService.GetTransactionReceipt(transferHash) == null)
                await Task.Delay(100);
            var result = await _ercService.GetBalanceForExternalTokenAsync(transferAddress, externalTokenAddress);
            //cashin to adapter
            var transferCashinHash = await _transferContractService.RecievePaymentFromTransferContract(transferAddress, coinAdapterAddress);
            while (await _transactionService.GetTransactionReceipt(transferCashinHash) == null)
                await Task.Delay(100);
        }
    }
}
