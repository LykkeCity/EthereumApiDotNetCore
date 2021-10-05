using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Lykke.Service.EthereumCore.Services
{
    public interface IOperationsService
    {
        void AddOperationToAbort(string operationId);
        bool IsOperationAborted(string operationId);
        List<string> GetAllOperationsToAbort();
    }

    public class OperationsService : IOperationsService
    {
        private readonly ConcurrentBag<string> _operationsToAbort = new ConcurrentBag<string>();

        public void AddOperationToAbort(string operationId)
        {
            if (_operationsToAbort.Contains(operationId))
                return;

            _operationsToAbort.Add(operationId);
        }

        public bool IsOperationAborted(string operationId)
        {
            return _operationsToAbort.Contains(operationId);
        }

        public List<string> GetAllOperationsToAbort()
        {
            return _operationsToAbort.ToList();
        }
    }
}
