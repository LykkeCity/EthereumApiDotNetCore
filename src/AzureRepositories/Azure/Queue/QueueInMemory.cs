using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureRepositories.Azure.Queue
{
    public class QueueInMemory<T> : IQueue<T> where T:class
    {
        private readonly Queue<T> _queue = new Queue<T>();

        private readonly object _lockObject = new object();

        public void PutMessage(T itm)
        {
            lock (_lockObject)
            {
                _queue.Enqueue(itm);
            }
        }

        public Task PutMessageAsync(T itm)
        {
            return Task.Run(() => PutMessage(itm));
        }

        public T GetMessage()
        {
            lock (_lockObject)
            {
                if (_queue.Count == 0)
                    return null;

                return _queue.Dequeue();
            }
        }

        public Task<T> GetMessageAsync()
        {
           return Task.Run(() => GetMessage());
        }

        public Task<QueueMessageToken<T>> GetMessageAndHideAsync()
        {
            throw new NotImplementedException();
        }

        public Task ProcessMessageAsync(QueueMessageToken<T> token)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> PeekAllMessages(int maxCount)
        {
            lock (_lockObject)
            {
                return _queue.ToArray();
            }
        }

        public Task<IEnumerable<T>> PeekAllMessagesAsync(int maxCount)
        {
            return Task.Run(() => PeekAllMessagesAsync(maxCount));
        }

        public void Clear()
        {
            lock (_lockObject)
            {
                _queue.Clear();
            }
        }

        public Task ClearAsync()
        {
            return Task.Run(() => Clear());
        }

        public int Size
        {
            get
            {
                lock (_lockObject)
                {
                    return _queue.Count;
                }
            }
        }

        public Task<int> GetSizeAsync()
        {
            return Task.Run(() => Size);
        }
    }
}
