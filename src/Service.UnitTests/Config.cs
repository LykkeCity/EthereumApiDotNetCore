using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lykke.Service.EthereumCore.Services;
using Common.Log;
using Lykke.Service.EthereumCore.Services.Signature;
using EthereumSamuraiApiCaller;
using Lykke.Service.EthereumCore.Services.Transactions;

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
            collection.AddSingleton<IEthereumSamuraiAPI, Mocks.MockEthereumSamuraiApi>();
            collection.AddSingleton<IEthereumSamuraiAPI, Mocks.MockEthereumSamuraiApi>();
            collection.AddSingleton<ISignatureChecker, SignatureChecker>();


            Services = collection.BuildServiceProvider();
        }
    }
}
