using System;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.PassTokenIntegration;
using Lykke.Service.EthereumCore.PassTokenIntegration.Models.Requests;

namespace Lykke.Service.PassClientTest.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            BlockPassClientFactory factory = new BlockPassClientFactory();
            var settings = new BlockPassClientSettings()
            {
                ApiKey = "",
                ServiceUrl = "https://activation-sandbox.blockpass.org"
            };
            var client = factory.CreateNew(settings, false);

            var address = new EthAddressRequest()
            {
                Address = "0x7ffe9d7f2864b6ab1d781dfa819e3cc4366a7cdf",
                AddressType = "eth"
            };

            var response = client.WhitelistAddressAsync(address).Result;

            System.Console.ReadLine();
        }
    }
}
