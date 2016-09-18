using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Log;
using Core.Timers;

namespace AzureRepositories.Azure.Queue
{
    public class QueueReader : TimerPeriod, IQueueReader
    {
        private readonly IQueueExt _queueExt;

        public QueueReader(IQueueExt queueExt, string componentName, int periodMs, ILog log) : base(componentName, periodMs, log)
        {
            _queueExt = queueExt;
        }

        public override async Task Execute()
        {
            var queueData = await _queueExt.GetMessageAsync();

            while (queueData != null)
            {
                try
                {
                    // if prehandler tells Skip the event - then we skip the event
                    if (_preHandler != null)
                        if (!await _preHandler(queueData.Data))
                        {
                            if (_errorHandlers.ContainsKey(queueData.Data.GetType()))
                            {
                                await _errorHandlers[queueData.Data.GetType()](queueData.Data);
                            }
                            continue;
                        }

                    //if was data is null => unregistered(unknown) type
                    if (queueData.Data == null)
                    {
                        continue;
                    }

                    var handler = _handlers[queueData.Data.GetType()];
                    await handler(queueData.Data);
                }
                finally
                {
                    await _queueExt.FinishMessageAsync(queueData);
                    queueData = await _queueExt.GetMessageAsync();

                }

            }
        }

        private readonly Dictionary<Type, Func<object, Task>> _handlers = new Dictionary<Type, Func<object, Task>>();
        public void RegisterHandler<T>(string id, Func<T, Task> handler)
        {
            _queueExt.RegisterTypes(QueueType.Create(id, typeof(T)));
            _handlers.Add(typeof(T), itm => handler((T)itm));
        }

        private readonly Dictionary<Type, Func<object, Task>> _errorHandlers = new Dictionary<Type, Func<object, Task>>();
        public void RegisterErrorHandler<T>(string id, Func<T, Task> handler)
        {
            _errorHandlers.Add(typeof(T), itm => handler((T)itm));
        }

        private Func<object, Task<bool>> _preHandler;

        public void RegisterPreHandler(Func<object, Task<bool>> preHandler)
        {
            _preHandler = preHandler;
        }
    }
}
