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
    //Warning: tests consumes ethereum on mainAccount. Run on testnet only!
    [TestClass]
    public class TestCoins : BaseTest
    {
        public static string _coinAddress = "0xa76a01048bd01d0ed236034b06a376a56812a765";
        public static string _clientTransferAddress = "0xfcd6ef45a385f7b027302ad484e1ceedea5a85dc";
        public static string _externalTokenAddress = "0x79e34063d05324e0bffc19901963d9ae5b101fba";
        private IBaseSettings _settings;
        private ICoinRepository _coinRepository;
        private IExchangeContractService _exchangeService;
        private IEthereumTransactionService _transactionService;
        private IErcInterfaceService _ercService;
        private ITransferContractService _transferContractService;

        [TestInitialize]
        public void Init()
        {
            _settings = Config.Services.GetService<IBaseSettings>();
            _coinRepository = Config.Services.GetService<ICoinRepository>();
            _exchangeService = Config.Services.GetService<IExchangeContractService>();
            _transactionService = Config.Services.GetService<IEthereumTransactionService>();
            _ercService = Config.Services.GetService<IErcInterfaceService>();
            _transferContractService = Config.Services.GetService<ITransferContractService>();
        }

        [TestMethod]
        public async Task TestCashinTokenFlow()
        {
            //Transfer to transition contract
            ICoin colorCoin = await _coinRepository.GetCoinByAddress(_coinAddress);
            BigInteger cashinAmount = new BigInteger(100);

            await CashinTokens(_externalTokenAddress, _clientTransferAddress, cashinAmount, _coinAddress, ClientA);
            string transferUser = await _transferContractService.GetTransferAddressUser(colorCoin.AdapterAddress, _clientTransferAddress);
            var currentBalance = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, ClientA);

            Assert.AreEqual(ClientA.ToLower(), transferUser);
            Assert.IsTrue(currentBalance >= cashinAmount);
        }

        [TestMethod]
        public async Task TestTransferTokens()
        {
            var colorCoin = await _coinRepository.GetCoinByAddress(_coinAddress);
            var toAddress = _settings.EthereumMainAccount;
            await CashinTokens(_externalTokenAddress, _clientTransferAddress, new BigInteger(100), _coinAddress, ClientA);
            var transferUser = await _transferContractService.GetTransferAddressUser(colorCoin.AdapterAddress, _clientTransferAddress);
            var currentBalance = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, ClientA);

            Assert.AreEqual(transferUser, ClientA.ToLower());

            var guid = Guid.NewGuid();
            EthUtils.GuidToBigInteger(guid);
            var strForHash = EthUtils.GuidToByteArray(guid).ToHex() +
                            colorCoin.AdapterAddress.HexToByteArray().ToHex() +
                            ClientA.HexToByteArray().ToHex() +
                            toAddress.HexToByteArray().ToHex() +
                            EthUtils.BigIntToArrayWithPadding(currentBalance).ToHex();

            var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());
            var externalSign = await _exchangeService.GetSign(guid, _coinAddress, ClientA, toAddress, currentBalance);
            byte[] signInBytes = externalSign.HexToByteArray().FixByteOrder();
            var transferHash = await _exchangeService.Transfer(guid, colorCoin.AdapterAddress, ClientA, toAddress,
                currentBalance, externalSign);

            while (await _transactionService.GetTransactionReceipt(transferHash) == null)
                await Task.Delay(100);

            var currentBalanceOnAdapter = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, ClientA);
            var newBalance = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, toAddress);

            Assert.IsTrue(await _transactionService.IsTransactionExecuted(transferHash, Constants.GasForCoinTransaction));
            Assert.IsTrue(currentBalanceOnAdapter == 0);
            Assert.IsTrue(currentBalance <= newBalance);
        }

        [TestMethod]
        public async Task TestCashoutTokens()
        {
            var colorCoin = await _coinRepository.GetCoinByAddress(_coinAddress);
            await CashinTokens(_externalTokenAddress, _clientTransferAddress, new BigInteger(100), _coinAddress, ClientA);
            var transferUser = await _transferContractService.GetTransferAddressUser(colorCoin.AdapterAddress, _clientTransferAddress);
            var oldBalance = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, ClientA);

            Assert.AreEqual(ClientA.ToLower(), transferUser);

            var guid = Guid.NewGuid();
            EthUtils.GuidToBigInteger(guid);
            var strForHash = EthUtils.GuidToByteArray(guid).ToHex() +
                            colorCoin.AdapterAddress.HexToByteArray().ToHex() +
                            ClientA.HexToByteArray().ToHex() +
                            ClientA.HexToByteArray().ToHex() +
                            EthUtils.BigIntToArrayWithPadding(oldBalance).ToHex();

            var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());
            //var solidityHash = await exchangeService.CalculateHash(guid, colorCoin.AdapterAddress, clientAddress, clientAddress, currentBalance);
            var sign = Sign(hash, PrivateKeyA).ToHex();
            var externalSign = await _exchangeService.GetSign(guid, _coinAddress, ClientA, ClientA, oldBalance);
            byte[] signInBytes = sign.HexToByteArray().FixByteOrder();
            //bool success = await exchangeService.CheckSign(clientAddress, hash, signInBytes);
             var cashout = await _exchangeService.CashOut(guid, colorCoin.AdapterAddress, ClientA, ClientA,
                 oldBalance, sign);

            while (await _transactionService.GetTransactionReceipt(cashout) == null)
                await Task.Delay(100);

            Assert.IsTrue(await _transactionService.IsTransactionExecuted(cashout, Constants.GasForCoinTransaction));
            var currentBalance = await _transferContractService.GetBalanceOnAdapter(colorCoin.AdapterAddress, ClientA);
            var newBalance = await _ercService.GetBalanceForExternalTokenAsync(ClientA, _externalTokenAddress);

            Assert.IsTrue(oldBalance <= newBalance);
            Assert.IsTrue(currentBalance == 0);
        }

        [TestMethod]
        public async Task TestCashoutEthereum()
        {
            //TODO: complete
        }

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
            var signature = key.SignAndCalculateV(hash);
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
