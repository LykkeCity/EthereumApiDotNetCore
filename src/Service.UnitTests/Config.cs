using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Service.EthereumCore.Services.Signature;
using EthereumSamuraiApiCaller;

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
