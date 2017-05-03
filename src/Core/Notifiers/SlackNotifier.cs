using AzureStorage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Notifiers
{
    public interface ISlackNotifier
    {
        Task WarningAsync(string message);
        Task ErrorAsync(string message);
        Task FinanceWarningAsync(string message);
    }

    public class SlackNotifier : ISlackNotifier
    {
        private readonly IQueueExt _queue;

        public SlackNotifier(Func<string, IQueueExt> queueFactory)
        {
            _queue = queueFactory(Constants.SlackNotifierQueue);
        }

        public async Task WarningAsync(string message)
        {
            var obj = new
            {
                Type = "Warnings",
                Sender = "chronobank service",
                Message = message
            };

            await _queue.PutRawMessageAsync(JsonConvert.SerializeObject(obj));
        }

        public async Task ErrorAsync(string message)
        {
            var obj = new
            {
                Type = "Errors",
                Sender = "chronobank service",
                Message = message
            };

            await _queue.PutRawMessageAsync(JsonConvert.SerializeObject(obj));
        }

        public async Task FinanceWarningAsync(string message)
        {
            var obj = new
            {
                Type = "Financewarnings",
                Sender = "chronobank service",
                Message = message
            };

            await _queue.PutRawMessageAsync(JsonConvert.SerializeObject(obj));
        }
    }
}
