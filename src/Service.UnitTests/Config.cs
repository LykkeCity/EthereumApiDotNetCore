using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Repositories;
using Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Services;
using Common.Log;
using Services.Signature;
using EthereumSamuraiApiCaller;
using Services.Transactions;

// ReSharper disable once CheckNamespace
namespace Service.UnitTests
{
    public class Config
    {
        public static IServiceProvider Services { get; set; }

        public async Task Initialize()
        {
            IServiceCollection collection = new ServiceCollection();
            collection.AddSingleton<INonceCalculator, Mocks.MockNonceCalculator>();
            collection.AddSingleton<IEthereumSamuraiApi, Mocks.MockEthereumSamuraiApi>();
            collection.AddSingleton<IEthereumSamuraiApi, Mocks.MockEthereumSamuraiApi>();
            collection.AddSingleton<ISignatureChecker, SignatureChecker>();
            //collection.AddSingleton<IRawTransactionSubmitter, RawTransactionSubmitter>();


            Services = collection.BuildServiceProvider();
        }
    }
}
