﻿//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Numerics;
//using System.Threading.Tasks;
//using Core;
//using Core.Repositories;
//using Core.Settings;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Microsoft.Extensions.DependencyInjection;
//using NBitcoin.Crypto;
//using Nethereum.ABI.Encoders;
//using Nethereum.ABI.Util;
//using Services;
//using Services.Coins;
//using Nethereum.Hex.HexConvertors.Extensions;
//using Nethereum.Web3;
//using Core.Utils;
//using Nethereum.Hex.HexTypes;
//using Nethereum.RPC.Eth.DTOs;
//using Newtonsoft.Json;
//using Services.Coins.Models;
//using AzureStorage.Queue;
//using Nethereum.Util;
//using Nethereum.Signer;

//namespace Tests
//{
//    //Todo: put tests on separate tables
//    //Warning: tests consumes ethereum on mainAccount. Run on testnet only!
//    [TestClass]
//    public class CoinEventServiceTest : BaseTest
//    {
//        private IBaseSettings _settings;
//        private ICoinEventService _coinEventService;

//        [TestInitialize]
//        public void Init()
//        {
//            _settings = Config.Services.GetService<IBaseSettings>();
//            _coinEventService = Config.Services.GetService<ICoinEventService>();
//        }

//        [TestMethod]
//        public async Task TestPublishEvent()
//        {
//            //await _coinEventService.PublishEvent(new CoinEvent("testEvent", "testEvent", "testEvent", "100", CoinEventType.CashinStarted, "testEvent") , false);
//            //var @event = await _coinEventService.GetCoinEvent("testEvent");
//        }
//    }
//}
