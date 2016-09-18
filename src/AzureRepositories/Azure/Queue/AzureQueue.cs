using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace AzureRepositories.Azure.Queue
{
    public class AzureQueue<T> : IQueue<T> where T : class
    {
        private CloudQueue _queue;

        public Task PutMessageAsync(T itm)
        {
            var msg = JsonConvert.SerializeObject(itm);
            return _queue.AddMessageAsync(new CloudQueueMessage(msg));
        }

        public async Task<T> GetMessageAsync()
        {
            var msg = await _queue.GetMessageAsync();
            if (msg == null)
                return null;

            await _queue.DeleteMessageAsync(msg);
            return JsonConvert.DeserializeObject<T>(msg.AsString);
        }

        public async Task<QueueMessageToken<T>> GetMessageAndHideAsync()
        {
            var msg = await _queue.GetMessageAsync();

            if (msg == null)
                return null;

            return QueueMessageToken<T>.Create(
                JsonConvert.DeserializeObject<T>(msg.AsString),
                msg
                );
        }

        public async Task ProcessMessageAsync(QueueMessageToken<T> msg)
        {
            var token = msg.Token as CloudQueueMessage;

            if (token == null)
                return;

            await _queue.DeleteMessageAsync(token);
        }

        public async Task<IEnumerable<T>> PeekAllMessagesAsync(int maxCount)
        {
            return
                (await _queue.PeekMessagesAsync(maxCount)).Select(msg => JsonConvert.DeserializeObject<T>(msg.AsString));
        }

        public Task ClearAsync()
        {
            return _queue.ClearAsync();
        }

        public async Task<int> GetSizeAsync()
        {
            var msg = (await _queue.PeekMessagesAsync(20)).ToArray();
            return msg.Length;
        }

        public Task CreateAsync(string conectionString, string queueName)
        {
            queueName = queueName.ToLower();
            var storageAccount = CloudStorageAccount.Parse(conectionString);
            var queueClient = storageAccount.CreateCloudQueueClient();

            _queue = queueClient.GetQueueReference(queueName);
            return _queue.CreateIfNotExistsAsync();
        }
    }
}