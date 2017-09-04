using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IOperationToHashMatch
    {
        string OperationId { get; set; }
        string TransactionHash { get; set; }
    }

    public class OperationToHashMatch : IOperationToHashMatch
    {
        public string OperationId { get; set; }
        public string TransactionHash { get; set; }
    }

    public interface IOperationToHashMatchRepository
    {
        Task<IOperationToHashMatch> GetByHashAsync(string transactionHash);
        Task<IOperationToHashMatch> GetAsync(string operationId);
        Task<IEnumerable<IOperationToHashMatch>> GetHistoricalForOperationAsync(string operationId);
        Task InsertOrReplaceAsync(IOperationToHashMatch match);
        Task InsertOrReplaceHistoricalAsync(IOperationToHashMatch match);
        Task ProcessAllAsync(Func<IEnumerable<IOperationToHashMatch>, Task> processAction);
        Task ProcessHistoricalAsync(string operationId, Func<IEnumerable<IOperationToHashMatch>, Task> processAction);
    }

    public interface ICreatePendingOperation
    {
        string OperationType { get; set; }
        string SignFrom { get; set; }
        string SignTo { get; set; }
        string CoinAdapterAddress { get; set; }
        string FromAddress { get; set; }
        string ToAddress { get; set; }
        string Amount { get; set; }
        string Change { get; set; }
    }

    public interface IPendingOperation : ICreatePendingOperation
    {
        string OperationId { get; set; }
    }

    public class PendingOperation : IPendingOperation
    {
        public string OperationId { get; set; }
        public string SignFrom { get; set; }
        public string SignTo { get; set; }
        public string CoinAdapterAddress { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public string Amount { get; set; }
        public string OperationType { get; set; }
        public Guid MainExchangeId { get; set; }
        public string Change { get; set; }
    }

    public interface IPendingOperationRepository
    {
        Task ProcessAllAsync(Func<IEnumerable<IPendingOperation>, Task> processAction);
        Task<IPendingOperation> GetOperation(string operationId);
        Task InsertOrReplace(IPendingOperation coin);
    }
}
