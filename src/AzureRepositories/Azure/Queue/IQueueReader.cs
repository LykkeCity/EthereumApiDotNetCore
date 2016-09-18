using System;
using System.Threading.Tasks;
using Core.Timers.Interfaces;

namespace AzureRepositories.Azure.Queue
{
    public interface IQueueReader : IStarter, ITimerCommand
    {
        void RegisterPreHandler(Func<object, Task<bool>> preHandler);
        void RegisterHandler<T>(string id, Func<T, Task> handler);
        void RegisterErrorHandler<T>(string id, Func<T, Task> handler);
        string GetComponentName();
    }
}
