using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IEventTrace
    {
        string OperationId { get; set; }
        DateTime TraceDate { get; set; }
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
