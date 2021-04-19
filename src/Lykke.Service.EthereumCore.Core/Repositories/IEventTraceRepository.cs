using System;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface IEventTrace
    {
        string OperationId { get; set; }
        DateTime TraceDate { get; }
        string Note {get;set;}
    }

    public class EventTrace : IEventTrace
    {
        public string OperationId {get;set;}
        public DateTime TraceDate {get;set;}
        public string Note { get; set; }
    }

    public interface IEventTraceRepository
    {
        Task InsertAsync(IEventTrace trace);
    }
}
