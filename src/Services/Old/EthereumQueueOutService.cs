using System;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core;
using Newtonsoft.Json;
using AzureStorage.Queue;
using Common.Log;

namespace Lykke.Service.EthereumCore.Services
{
    public interface IEthereumQueueOutService
    {
        Task FirePaymentEvent(string userContract, decimal amount, string trHash);
    }

    public class EthereumQueueOutService : IEthereumQueueOutService
    {
        private readonly IQueueExt _queue;
        private readonly ILog _logger;

        public EthereumQueueOutService(Func<string, IQueueExt> queueFactory, ILog logger)
        {
            _queue = queueFactory(Constants.EthereumOutQueue);
            _logger = logger;
        }

        /// <summary>
        /// Sends event for ethereum payment to azure queue
        /// </summary>
        /// <param name="userContract"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public async Task FirePaymentEvent(string userContract, decimal amount, string trHash)
        {
            try
            {
                var model = new EthereumCashInModel
                {
                    Amount = amount,
                    Contract = userContract,
                    TransactionHash = trHash
                };

                var json = JsonConvert.SerializeObject(model);

                await _queue.PutRawMessageAsync(json);
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync("ApiCallService", "FirePaymentEvent", $"Contract : {userContract}, amount: {amount}", e);
            }
        }
    }

    public class EthereumCashInModel
    {
        public string Type => "EthereumCashIn";
        public decimal Amount { get; set; }
        public string Contract { get; set; }
        public string TransactionHash { get; set; }
    }
}
