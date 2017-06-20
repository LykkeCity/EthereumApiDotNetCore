using AzureStorage.Queue;
using Core;
using Core.Notifiers;
using Lykke.JobTriggers.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AzureRepositories.Notifiers
{
    public class SlackNotifier : ISlackNotifier, IPoisionQueueNotifier
    {
        private readonly IQueueExt _queue;
        private const string _sender = "ethereumcoreservice";

        public SlackNotifier(Func<string, IQueueExt> queueFactory)
        {
            _queue = queueFactory(Constants.SlackNotifierQueue);
        }

        public async Task WarningAsync(string message)
        {
            var obj = new
            {
                Type = "Warnings",
                Sender = _sender,
                Message = message
            };

            await _queue.PutRawMessageAsync(JsonConvert.SerializeObject(obj));
        }

        public async Task ErrorAsync(string message)
        {
            var obj = new
            {
                Type = "Ethereum",//"Errors",
                Sender = _sender,
                Message = message
            };

            await _queue.PutRawMessageAsync(JsonConvert.SerializeObject(obj));
        }

        public async Task FinanceWarningAsync(string message)
        {
            var obj = new
            {
                Type = "Financewarnings",
                Sender = _sender,
                Message = message
            };

            await _queue.PutRawMessageAsync(JsonConvert.SerializeObject(obj));
        }

        public Task NotifyAsync(string message)
        {
            return ErrorAsync(message);
        }
    }
}
