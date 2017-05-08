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
using Nethereum.Core.Signing.Crypto;
using Nethereum.Web3;
using Core.Utils;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;
using Services.Coins.Models;
using AzureStorage.Queue;

namespace Tests
{
    [TestClass]
    public class TestCoins : BaseTest
    {
        [TestMethod]
        public async Task TestCashin()
        {
            var coinService = Config.Services.GetService<IExchangeContractService>();
            var coinRepository = Config.Services.GetService<ICoinRepository>();
            var colorCoin = await coinRepository.GetCoin(ColorCoin);

            var cointTransactionService = Config.Services.GetService<ICoinTransactionService>();
            var transactionService = Config.Services.GetService<IEthereumTransactionService>();
            var currentBalance = await coinService.GetBalance(colorCoin.Id, ClientA);

            var amount = 100m;

            var result = await coinService.CashIn(Guid.NewGuid(), colorCoin.Id, ClientA, amount);

            while (await transactionService.GetTransactionReceipt(result) == null)
                await Task.Delay(100);



            Assert.IsTrue(await cointTransactionService.ProcessTransaction());

            var newBalance = await coinService.GetBalance(colorCoin.Id, ClientA);

            Assert.AreEqual(currentBalance + amount.ToBlockchainAmount(colorCoin.Multiplier), newBalance);
        }

        [TestMethod]
        public async Task TestCashoutTokens()
        {
            var clientPrivateKey = "0x1149984b590c0bcd88ca4e7ef80d2f4aa7b0bc0f52ac7895068e89262c8733c6";
            var coinAddress = "0xd12ecd4779ed86762b90f04d9315daa1e1dc3ab7";
            var clientAddress = "0x725b6b6f72dfdc16f56cb36b4a0227151b80a9fc";
            var clientTransferAddress = "0x8d90f8805416403763dd0e58dd3c9b7c427ca4ae";
            var coinRepository = Config.Services.GetService<ICoinRepository>();
            var colorCoin = await coinRepository.GetCoinByAddress(coinAddress);
            var exchangeService = Config.Services.GetService<IExchangeContractService>();
            var transactionService = Config.Services.GetService<IEthereumTransactionService>();

            await exchangeService.CashIn(Guid.NewGuid(), coinAddress, clientTransferAddress, 2048);
            var transferUser = await exchangeService.GetTransferAddressUser(colorCoin.AdapterAddress, clientTransferAddress);
            var currentBalance = await exchangeService.GetBalance(colorCoin.AdapterAddress, transferUser);

            var amount = 512m;

            var midBalance = await exchangeService.GetBalance(colorCoin.AdapterAddress, clientAddress);

            Assert.AreEqual(currentBalance + amount.ToBlockchainAmount(colorCoin.Multiplier), midBalance);

            var guid = Guid.NewGuid();

            var strForHash = EthUtils.GuidToByteArray(guid).ToHex() +
                            colorCoin.AdapterAddress.HexToByteArray().ToHex() +
                            clientAddress.HexToByteArray().ToHex() +
                            clientAddress.HexToByteArray().ToHex() +
                            EthUtils.BigIntToArrayWithPadding(amount.ToBlockchainAmount(colorCoin.Multiplier)).ToHex();

            var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());

            var sign = Sign(hash, clientPrivateKey).ToHex();
            var cashout = await exchangeService.CashOut(guid, colorCoin.AdapterAddress, clientAddress, clientAddress,
                amount, sign);

            while (await transactionService.GetTransactionReceipt(cashout) == null)
                await Task.Delay(100);

            Assert.IsTrue(await transactionService.IsTransactionExecuted(cashout, Constants.GasForCoinTransaction));

            var newBalance = await exchangeService.GetBalance(colorCoin.Id, ClientA);

            Assert.AreEqual(currentBalance, newBalance);
        }

        [TestMethod]
        public async Task TestCashoutEthereum()
        {
            //TODO: complete
        }

        [TestMethod]
        public async Task TestTransfer()
        {
            var coinRepository = Config.Services.GetService<ICoinRepository>();
            var colorCoin = await coinRepository.GetCoin(ColorCoin);

            var coinService = Config.Services.GetService<IExchangeContractService>();
            var transactionService = Config.Services.GetService<IEthereumTransactionService>();

            var currentBalance = await coinService.GetBalance(colorCoin.Id, ClientA);

            var amount = 100m;

            var result = await coinService.CashIn(Guid.NewGuid(), colorCoin.Id, ClientA, amount);

            while (await transactionService.GetTransactionReceipt(result) == null)
                await Task.Delay(100);

            var midBalance = await coinService.GetBalance(colorCoin.Id, ClientA);

            Assert.AreEqual(currentBalance + amount.ToBlockchainAmount(colorCoin.Multiplier), midBalance);

            var clientBBalance = await coinService.GetBalance(colorCoin.Id, ClientB);

            var guid = Guid.NewGuid();

            var strForHash = EthUtils.GuidToByteArray(guid).ToHex() +
                            colorCoin.AdapterAddress.HexToByteArray().ToHex() +
                            ClientA.HexToByteArray().ToHex() +
                            ClientB.HexToByteArray().ToHex() +
                            EthUtils.BigIntToArrayWithPadding(amount.ToBlockchainAmount(colorCoin.Multiplier)).ToHex();

            var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());

            var sign = Sign(hash, PrivateKeyA).ToHex();
            var transfer = await coinService.Transfer(guid, colorCoin.Id, ClientA, ClientB, amount, sign);

            while (await transactionService.GetTransactionReceipt(transfer) == null)
                await Task.Delay(100);

            Assert.IsTrue(await transactionService.IsTransactionExecuted(transfer, Constants.GasForCoinTransaction));

            var newBalance = await coinService.GetBalance(colorCoin.Id, ClientA);
            var newClientBBalance = await coinService.GetBalance(colorCoin.Id, ClientB);

            Assert.AreEqual(currentBalance, newBalance);
            Assert.AreEqual(clientBBalance + amount.ToBlockchainAmount(colorCoin.Multiplier), newClientBBalance);
        }


        [TestMethod]
        public async Task TestCoinSwap()
        {
            var coinRepository = Config.Services.GetService<ICoinRepository>();
            var colorCoin = await coinRepository.GetCoin(ColorCoin);
            var ethCoin = await coinRepository.GetCoin(EthCoin);

            var coinService = Config.Services.GetService<IExchangeContractService>();
            var transactionService = Config.Services.GetService<IEthereumTransactionService>();

            var currentBalance_a = await coinService.GetBalance(colorCoin.Id, ClientA);
            var currentBalance_b = await coinService.GetBalance(ethCoin.Id, ClientB);

            var amount_a = 100m;
            var amount_b = 0.01M;

            var cashin_a = await coinService.CashIn(Guid.NewGuid(), colorCoin.Id, ClientA, amount_a);
            var cashin_b = await coinService.CashIn(Guid.NewGuid(), ethCoin.Id, ClientB, amount_b);

            while (await transactionService.GetTransactionReceipt(cashin_a) == null)
                await Task.Delay(100);

            while (await transactionService.GetTransactionReceipt(cashin_b) == null)
                await Task.Delay(100);

            var midBalance_a = await coinService.GetBalance(colorCoin.Id, ClientA);
            var midBalance_b = await coinService.GetBalance(ethCoin.Id, ClientB);

            Assert.AreEqual(currentBalance_a + amount_a.ToBlockchainAmount(colorCoin.Multiplier), midBalance_a);
            Assert.AreEqual(currentBalance_b + amount_b.ToBlockchainAmount(ethCoin.Multiplier), midBalance_b);

            var swap_amount_a = 50m;
            var swap_amount_b = 0.01m;

            var guid = Guid.NewGuid();

            var strForHash = EthUtils.GuidToByteArray(guid).ToHex() +
                            ClientA.HexToByteArray().ToHex() +
                            ClientB.HexToByteArray().ToHex() +
                            colorCoin.AdapterAddress.HexToByteArray().ToHex() +
                            ethCoin.AdapterAddress.HexToByteArray().ToHex() +
                            EthUtils.BigIntToArrayWithPadding(swap_amount_a.ToBlockchainAmount(colorCoin.Multiplier)).ToHex() +
                            EthUtils.BigIntToArrayWithPadding(swap_amount_b.ToBlockchainAmount(ethCoin.Multiplier)).ToHex();

            var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());

            var sign_a = Sign(hash, PrivateKeyA).ToHex();
            var sign_b = Sign(hash, PrivateKeyB).ToHex();

            var swap = await coinService.Swap(guid, ClientA, ClientB, colorCoin.Id, ethCoin.Id, swap_amount_a, swap_amount_b, sign_a, sign_b);

            while (await transactionService.GetTransactionReceipt(swap) == null)
                await Task.Delay(100);

            Assert.IsTrue(await transactionService.IsTransactionExecuted(swap, Constants.GasForCoinTransaction));

            var newBalance_a = await coinService.GetBalance(colorCoin.Id, ClientA);
            var newBalance_b = await coinService.GetBalance(ethCoin.Id, ClientB);

            Assert.AreEqual(midBalance_a - swap_amount_a.ToBlockchainAmount(colorCoin.Multiplier), newBalance_a);
            Assert.AreEqual(midBalance_b - swap_amount_b.ToBlockchainAmount(ethCoin.Multiplier), newBalance_b);
        }


        [TestMethod]
        public async Task TestCoinEvents()
        {
            var coinRepository = Config.Services.GetService<ICoinRepository>();
            var colorCoin = await coinRepository.GetCoin(ColorCoin);
            var ethCoin = await coinRepository.GetCoin(EthCoin);

            var coinService = Config.Services.GetService<IExchangeContractService>();
            var transactionService = Config.Services.GetService<IEthereumTransactionService>();
            var queueFactory = Config.Services.GetService<Func<string, IQueueExt>>();
            var eventQueue = queueFactory(Constants.CoinEventQueue);

            await coinService.GetCoinContractFilters(true);
            decimal amountColor = 2, amountEth = 0.02M, amountColorOut = 1, amountEthOut = 0.01M, amountColorSwap = 0.5M, amountEthSwap = 0.005M;

            var cashin1 = await coinService.CashIn(Guid.NewGuid(), colorCoin.Id, ClientA, amountColor);
            var cashin2 = await coinService.CashIn(Guid.NewGuid(), ethCoin.Id, ClientB, amountEth);

            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();

            var strForHash = EthUtils.GuidToByteArray(guid1).ToHex() +
                        colorCoin.AdapterAddress.HexToByteArray().ToHex() +
                        ClientA.HexToByteArray().ToHex() +
                        ClientB.HexToByteArray().ToHex() +
                        EthUtils.BigIntToArrayWithPadding(amountColorOut.ToBlockchainAmount(colorCoin.Multiplier)).ToHex();
            var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());


            var strForHash2 = EthUtils.GuidToByteArray(guid2).ToHex() +
                    ethCoin.AdapterAddress.HexToByteArray().ToHex() +
                    ClientB.HexToByteArray().ToHex() +
                    ClientA.HexToByteArray().ToHex() +
                    EthUtils.BigIntToArrayWithPadding(amountEthOut.ToBlockchainAmount(ethCoin.Multiplier)).ToHex();
            var hash2 = new Sha3Keccack().CalculateHash(strForHash2.HexToByteArray());

            var cashOut1 = await coinService.CashOut(guid1, colorCoin.Id, ClientA, ClientB, amountColorOut, Sign(hash, PrivateKeyA).ToHex());
            var cashOut2 = await coinService.CashOut(guid2, ethCoin.Id, ClientB, ClientA, amountEthOut, Sign(hash2, PrivateKeyB).ToHex());

            var swapGuid = Guid.NewGuid();

            var strForHash3 = EthUtils.GuidToByteArray(swapGuid).ToHex() +
                            ClientA.HexToByteArray().ToHex() +
                            ClientB.HexToByteArray().ToHex() +
                            colorCoin.AdapterAddress.HexToByteArray().ToHex() +
                            ethCoin.AdapterAddress.HexToByteArray().ToHex() +
                            EthUtils.BigIntToArrayWithPadding(amountColorSwap.ToBlockchainAmount(colorCoin.Multiplier)).ToHex() +
                            EthUtils.BigIntToArrayWithPadding(amountEthSwap.ToBlockchainAmount(ethCoin.Multiplier)).ToHex();
            var hash3 = new Sha3Keccack().CalculateHash(strForHash3.HexToByteArray());

            var swap3 = await coinService.Swap(swapGuid, ClientA, ClientB, colorCoin.Id, ethCoin.Id, amountColorSwap, amountEthSwap,
                Sign(hash3, PrivateKeyA).ToHex(), Sign(hash3, PrivateKeyB).ToHex());


            var transactions = new List<string> { cashin1, cashin2, cashOut1, cashOut2, swap3 };


            while (transactions.Count > 0)
            {
                foreach (var trHash in transactions.ToList())
                {
                    if ((await transactionService.GetTransactionReceipt(trHash)) != null)
                        transactions.Remove(trHash);
                }
                await Task.Delay(100);
            }

            await coinService.RetrieveEventLogs(false);

            var messages = new List<CoinContractPublicEvent>();
            for (int i = 0; i < 6; i++)
            {
                var msg = await eventQueue.GetRawMessageAsync();
                if (msg != null)
                    messages.Add(JsonConvert.DeserializeObject<CoinContractPublicEvent>(msg.AsString));
            }

            Assert.IsTrue(messages.Any(o => o.EventName == Constants.CashInEvent && o.Address == colorCoin.AdapterAddress && o.Amount == amountColor && o.Caller == ClientA),
                "not found cashin1");
            Assert.IsTrue(messages.Any(o => o.EventName == Constants.CashInEvent && o.Address == ethCoin.AdapterAddress && o.Amount == amountEth && o.Caller == ClientB),
                "not found cashin2");
            Assert.IsTrue(messages.Any(o => o.EventName == Constants.CashOutEvent && o.Address == colorCoin.AdapterAddress && o.Amount == amountColorOut && o.From == ClientA),
                "not found cashout1");
            Assert.IsTrue(messages.Any(o => o.EventName == Constants.CashOutEvent && o.Address == ethCoin.AdapterAddress && o.Amount == amountEthOut && o.From == ClientB),
                "not found cashout2");
            Assert.IsTrue(messages.Any(o => o.EventName == Constants.TransferEvent && o.Address == colorCoin.AdapterAddress && o.Amount == amountColorSwap && o.From == ClientA && o.To == ClientB),
                "not found swap1");
            Assert.IsTrue(messages.Any(o => o.EventName == Constants.TransferEvent && o.Address == ethCoin.AdapterAddress && o.Amount == amountEthSwap && o.From == ClientB && o.To == ClientA),
                "not found swap2");
        }

        private byte[] Sign(byte[] hash, string privateKey)
        {
            var key = new ECKey(privateKey.HexToByteArray(), true);
            var signature = key.SignAndCalculateV(hash);

            var r = signature.R.ToByteArrayUnsigned().ToHex();
            var s = signature.S.ToByteArrayUnsigned().ToHex();
            var v = new[] { signature.V }.ToHex();

            var arr = (r + s + v).HexToByteArray();
            return arr;
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
