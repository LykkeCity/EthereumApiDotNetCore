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
using NBitcoin.Crypto;
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

namespace Tests
{
    //Todo: put tests on separate tables
    //Warning: tests consumes ethereum on mainAccount. Run on testnet only!
    [TestClass]
    public class ExchangeServiceTests : BaseTest
    {
        public static string _ethereumAdapterAddress = "";
        public static string _clientEthereumTransferAddress = "";

        //BCAPTokenAddress -         0xce2ef46ecc168226f33b6f6b8a56e90450d0d2c0
        //BCAPTokenAdapter-	   0x1e8e8ccbd9a7a8d82875054aa8342159d96356a9
        //BCAPTransferAddress -      0x7ff01d3225726eb3dd3356fc57e71e5ec0aab042

        protected static string _tokenAdapterAddress = "0x1e8e8ccbd9a7a8d82875054aa8342159d96356a9";//"0x27b1ad3f1ae08eec8205bcbe91166b6387d67c4f";
        protected static string _clientTokenTransferAddress = "0x7ff01d3225726eb3dd3356fc57e71e5ec0aab042";//"0x967ddcf62c2ecec1c4d231c7498c287b857846e7";
        protected static string _externalTokenAddress = "0xce2ef46ecc168226f33b6f6b8a56e90450d0d2c0";//"0x79e34063d05324e0bffc19901963d9ae5b101fba";
        protected static string _ethereumCoinOwnerB = "0xd513BeA430322c488600Af6eE094aB32238C7169";
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
            var result = await _exchangeService.CheckSign(guid, _tokenAdapterAddress, _clientA, _clientA, new BigInteger(amount), sign);

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
            var result = await _exchangeService.CheckSign(guid, _ethereumAdapterAddress, _clientA, _clientA, new BigInteger(amount - 1), sign);

            Assert.IsFalse(result);
        }

        //[TestMethod]
        //public async Task TestCheckId_IsInList()
        //{
        //    var guid = Guid.Parse("936fc8e3-43fd-468f-8b13-cb948520bb53");
        //    var result = await _exchangeService.CheckId(guid);

        //    Assert.IsTrue(result);
        //}

        #endregion
        //[TestMethod]
        //public async Task TestTransfer()
        //{
        //    var coinRepository = Config.Services.GetService<ICoinRepository>();
        //    var colorCoin = await coinRepository.GetCoin(ColorCoin);

        //    var coinService = Config.Services.GetService<IExchangeContractService>();
        //    var transactionService = Config.Services.GetService<IEthereumTransactionService>();

        //    var currentBalance = await coinService.GetBalance(colorCoin.Id, ClientA);

        //    BigInteger amount = new BigInteger(100);

        //    var result = await coinService.CashIn(Guid.NewGuid(), colorCoin.Id, ClientA, amount);

        //    while (await transactionService.GetTransactionReceipt(result) == null)
        //        await Task.Delay(100);

        //    var midBalance = await coinService.GetBalance(colorCoin.Id, ClientA);

        //    Assert.AreEqual(currentBalance + amount, midBalance);

        //    var clientBBalance = await coinService.GetBalance(colorCoin.Id, ClientB);

        //    var guid = Guid.NewGuid();

        //    var strForHash = EthUtils.GuidToByteArray(guid).ToHex() +
        //                    colorCoin.AdapterAddress.HexToByteArray().ToHex() +
        //                    ClientA.HexToByteArray().ToHex() +
        //                    ClientB.HexToByteArray().ToHex() +
        //                    EthUtils.BigIntToArrayWithPadding(amount).ToHex();

        //    var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());

        //    var sign = Sign(hash, PrivateKeyA).ToHex();
        //    var transfer = await coinService.Transfer(guid, colorCoin.Id, ClientA, ClientB, amount, sign);

        //    while (await transactionService.GetTransactionReceipt(transfer) == null)
        //        await Task.Delay(100);

        //    Assert.IsTrue(await transactionService.IsTransactionExecuted(transfer, Constants.GasForCoinTransaction));

        //    var newBalance = await coinService.GetBalance(colorCoin.Id, ClientA);
        //    var newClientBBalance = await coinService.GetBalance(colorCoin.Id, ClientB);

        //    Assert.AreEqual(currentBalance, newBalance);
        //    Assert.AreEqual(clientBBalance + amount.ToBlockchainAmount(colorCoin.Multiplier), newClientBBalance);
        //}


        //[TestMethod]
        //public async Task TestCoinSwap()
        //{
        //    var coinRepository = Config.Services.GetService<ICoinRepository>();
        //    var colorCoin = await coinRepository.GetCoin(ColorCoin);
        //    var ethCoin = await coinRepository.GetCoin(EthCoin);

        //    var coinService = Config.Services.GetService<IExchangeContractService>();
        //    var transactionService = Config.Services.GetService<IEthereumTransactionService>();

        //    var currentBalance_a = await coinService.GetBalance(colorCoin.Id, ClientA);
        //    var currentBalance_b = await coinService.GetBalance(ethCoin.Id, ClientB);

        //    BigInteger amount_a = new BigInteger(100);
        //    BigInteger amount_b = new BigInteger(100);

        //    var cashin_a = await coinService.CashIn(Guid.NewGuid(), colorCoin.Id, ClientA, amount_a);
        //    var cashin_b = await coinService.CashIn(Guid.NewGuid(), ethCoin.Id, ClientB, amount_b);

        //    while (await transactionService.GetTransactionReceipt(cashin_a) == null)
        //        await Task.Delay(100);

        //    while (await transactionService.GetTransactionReceipt(cashin_b) == null)
        //        await Task.Delay(100);

        //    var midBalance_a = await coinService.GetBalance(colorCoin.Id, ClientA);
        //    var midBalance_b = await coinService.GetBalance(ethCoin.Id, ClientB);

        //    Assert.AreEqual(currentBalance_a + amount_a, midBalance_a);
        //    Assert.AreEqual(currentBalance_b + amount_b, midBalance_b);

        //    var swap_amount_a = 50m;
        //    var swap_amount_b = 0.01m;

        //    var guid = Guid.NewGuid();

        //    var strForHash = EthUtils.GuidToByteArray(guid).ToHex() +
        //                    ClientA.HexToByteArray().ToHex() +
        //                    ClientB.HexToByteArray().ToHex() +
        //                    colorCoin.AdapterAddress.HexToByteArray().ToHex() +
        //                    ethCoin.AdapterAddress.HexToByteArray().ToHex() +
        //                    EthUtils.BigIntToArrayWithPadding(swap_amount_a.ToBlockchainAmount(colorCoin.Multiplier)).ToHex() +
        //                    EthUtils.BigIntToArrayWithPadding(swap_amount_b.ToBlockchainAmount(ethCoin.Multiplier)).ToHex();

        //    var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());

        //    var sign_a = Sign(hash, PrivateKeyA).ToHex();
        //    var sign_b = Sign(hash, PrivateKeyB).ToHex();

        //    var swap = await coinService.Swap(guid, ClientA, ClientB, colorCoin.Id, ethCoin.Id, swap_amount_a, swap_amount_b, sign_a, sign_b);

        //    while (await transactionService.GetTransactionReceipt(swap) == null)
        //        await Task.Delay(100);

        //    Assert.IsTrue(await transactionService.IsTransactionExecuted(swap, Constants.GasForCoinTransaction));

        //    var newBalance_a = await coinService.GetBalance(colorCoin.Id, ClientA);
        //    var newBalance_b = await coinService.GetBalance(ethCoin.Id, ClientB);

        //    Assert.AreEqual(midBalance_a - swap_amount_a.ToBlockchainAmount(colorCoin.Multiplier), newBalance_a);
        //    Assert.AreEqual(midBalance_b - swap_amount_b.ToBlockchainAmount(ethCoin.Multiplier), newBalance_b);
        //}


        //[TestMethod]
        //public async Task TestCoinEvents()
        //{
        //    var coinRepository = Config.Services.GetService<ICoinRepository>();
        //    var colorCoin = await coinRepository.GetCoin(ColorCoin);
        //    var ethCoin = await coinRepository.GetCoin(EthCoin);

        //    var coinService = Config.Services.GetService<IExchangeContractService>();
        //    var transactionService = Config.Services.GetService<IEthereumTransactionService>();
        //    var queueFactory = Config.Services.GetService<Func<string, IQueueExt>>();
        //    var eventQueue = queueFactory(Constants.CoinEventQueue);

        //    await coinService.GetCoinContractFilters(true);
        //    decimal amountColor = 2, amountEth = 0.02M, amountColorOut = 1, amountEthOut = 0.01M, amountColorSwap = 0.5M, amountEthSwap = 0.005M;

        //    var cashin1 = await coinService.CashIn(Guid.NewGuid(), colorCoin.Id, ClientA, amountColor);
        //    var cashin2 = await coinService.CashIn(Guid.NewGuid(), ethCoin.Id, ClientB, amountEth);

        //    var guid1 = Guid.NewGuid();
        //    var guid2 = Guid.NewGuid();

        //    var strForHash = EthUtils.GuidToByteArray(guid1).ToHex() +
        //                colorCoin.AdapterAddress.HexToByteArray().ToHex() +
        //                ClientA.HexToByteArray().ToHex() +
        //                ClientB.HexToByteArray().ToHex() +
        //                EthUtils.BigIntToArrayWithPadding(amountColorOut.ToBlockchainAmount(colorCoin.Multiplier)).ToHex();
        //    var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());


        //    var strForHash2 = EthUtils.GuidToByteArray(guid2).ToHex() +
        //            ethCoin.AdapterAddress.HexToByteArray().ToHex() +
        //            ClientB.HexToByteArray().ToHex() +
        //            ClientA.HexToByteArray().ToHex() +
        //            EthUtils.BigIntToArrayWithPadding(amountEthOut.ToBlockchainAmount(ethCoin.Multiplier)).ToHex();
        //    var hash2 = new Sha3Keccack().CalculateHash(strForHash2.HexToByteArray());

        //    var cashOut1 = await coinService.CashOut(guid1, colorCoin.Id, ClientA, ClientB, amountColorOut, Sign(hash, PrivateKeyA).ToHex());
        //    var cashOut2 = await coinService.CashOut(guid2, ethCoin.Id, ClientB, ClientA, amountEthOut, Sign(hash2, PrivateKeyB).ToHex());

        //    var swapGuid = Guid.NewGuid();

        //    var strForHash3 = EthUtils.GuidToByteArray(swapGuid).ToHex() +
        //                    ClientA.HexToByteArray().ToHex() +
        //                    ClientB.HexToByteArray().ToHex() +
        //                    colorCoin.AdapterAddress.HexToByteArray().ToHex() +
        //                    ethCoin.AdapterAddress.HexToByteArray().ToHex() +
        //                    EthUtils.BigIntToArrayWithPadding(amountColorSwap.ToBlockchainAmount(colorCoin.Multiplier)).ToHex() +
        //                    EthUtils.BigIntToArrayWithPadding(amountEthSwap.ToBlockchainAmount(ethCoin.Multiplier)).ToHex();
        //    var hash3 = new Sha3Keccack().CalculateHash(strForHash3.HexToByteArray());

        //    var swap3 = await coinService.Swap(swapGuid, ClientA, ClientB, colorCoin.Id, ethCoin.Id, amountColorSwap, amountEthSwap,
        //        Sign(hash3, PrivateKeyA).ToHex(), Sign(hash3, PrivateKeyB).ToHex());


        //    var transactions = new List<string> { cashin1, cashin2, cashOut1, cashOut2, swap3 };


        //    while (transactions.Count > 0)
        //    {
        //        foreach (var trHash in transactions.ToList())
        //        {
        //            if ((await transactionService.GetTransactionReceipt(trHash)) != null)
        //                transactions.Remove(trHash);
        //        }
        //        await Task.Delay(100);
        //    }

        //    await coinService.RetrieveEventLogs(false);

        //    var messages = new List<CoinContractPublicEvent>();
        //    for (int i = 0; i < 6; i++)
        //    {
        //        var msg = await eventQueue.GetRawMessageAsync();
        //        if (msg != null)
        //            messages.Add(JsonConvert.DeserializeObject<CoinContractPublicEvent>(msg.AsString));
        //    }

        //    Assert.IsTrue(messages.Any(o => o.EventName == Constants.CashInEvent && o.Address == colorCoin.AdapterAddress && o.Amount == amountColor && o.Caller == ClientA),
        //        "not found cashin1");
        //    Assert.IsTrue(messages.Any(o => o.EventName == Constants.CashInEvent && o.Address == ethCoin.AdapterAddress && o.Amount == amountEth && o.Caller == ClientB),
        //        "not found cashin2");
        //    Assert.IsTrue(messages.Any(o => o.EventName == Constants.CashOutEvent && o.Address == colorCoin.AdapterAddress && o.Amount == amountColorOut && o.From == ClientA),
        //        "not found cashout1");
        //    Assert.IsTrue(messages.Any(o => o.EventName == Constants.CashOutEvent && o.Address == ethCoin.AdapterAddress && o.Amount == amountEthOut && o.From == ClientB),
        //        "not found cashout2");
        //    Assert.IsTrue(messages.Any(o => o.EventName == Constants.TransferEvent && o.Address == colorCoin.AdapterAddress && o.Amount == amountColorSwap && o.From == ClientA && o.To == ClientB),
        //        "not found swap1");
        //    Assert.IsTrue(messages.Any(o => o.EventName == Constants.TransferEvent && o.Address == ethCoin.AdapterAddress && o.Amount == amountEthSwap && o.From == ClientB && o.To == ClientA),
        //        "not found swap2");
        //}

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
        //[Test]
        //public async Task TestTransferContract()
        //{
        //    var settings = Config.Services.GetService<IBaseSettings>();
        //    var coinService = Config.Services.GetService<ICoinContractService>();

        //    var coinRepository = Config.Services.GetService<ICoinRepository>();
        //    var coin = await coinRepository.GetCoin(settings.EthCoin);

        //    var ethereumtransactionService = Config.Services.GetService<IEthereumTransactionService>();

        //    var amount = 0.1M;
        //    var web3 = new Web3(settings.EthereumUrl);

        //    var initBalance = UnitConversion.Convert.FromWei(await web3.Eth.GetBalance.SendRequestAsync(settings.TransferContract.Address));

        //    await web3.Personal.UnlockAccount.SendRequestAsync(settings.EthereumMainAccount, settings.EthereumMainAccountPassword, new HexBigInteger(120));
        //    var tr = await web3.Eth.Transactions.SendTransaction.SendRequestAsync(new TransactionInput(null, settings.TransferContract.Address, settings.EthereumMainAccount, new HexBigInteger(Constants.GasForUserContractTransafer), new HexBigInteger(UnitConversion.Convert.ToWei(amount))));

        //    while (await ethereumtransactionService.GetTransactionReceipt(tr) == null)
        //        await Task.Delay(100);

        //    var newBalance =
        //        UnitConversion.Convert.FromWei(await web3.Eth.GetBalance.SendRequestAsync(settings.TransferContract.Address));

        //    Assert.AreEqual(initBalance + amount, newBalance);

        //    var clientABalance = await coinService.GetBalance(settings.EthCoin, ClientA);

        //    tr = await coinService.CashinOverTransferContract(Guid.NewGuid(), settings.EthCoin, ClientA, amount);

        //    while (await ethereumtransactionService.GetTransactionReceipt(tr) == null)
        //        await Task.Delay(100);

        //    var newClientABalance = await coinService.GetBalance(settings.EthCoin, ClientA);

        //    Assert.AreEqual(clientABalance + amount.ToBlockchainAmount(coin.Multiplier), newClientABalance);

        //    newBalance = UnitConversion.Convert.FromWei(await web3.Eth.GetBalance.SendRequestAsync(settings.TransferContract.Address));

        //    Assert.AreEqual(initBalance, newBalance);
        //}
    }
}
