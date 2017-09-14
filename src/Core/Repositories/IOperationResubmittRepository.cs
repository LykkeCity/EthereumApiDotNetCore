using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IOperationResubmitt
    {
        string OperationId { get; set; }
        int ResubmittCount { get; set; }
    }

    public class OperationResubmitt : IOperationResubmitt
    {
        public string OperationId { get; set; }
        public int ResubmittCount { get; set; }
    }

    public interface IOperationResubmittRepository
    {
        Task<IOperationResubmitt> GetAsync(string operationId);
        Task InsertOrReplaceAsync(IOperationResubmitt match);
    }
}
