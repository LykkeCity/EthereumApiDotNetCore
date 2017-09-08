using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IUserAssignmentFail
    {
        string ContractAddress { get; set; }
        int FailCount { get; set; }
        bool CanBeRestoredInternally { get; set; }
    }

    public class UserAssignmentFail : IUserAssignmentFail
    {
        public string ContractAddress { get; set; }
        public int FailCount { get; set; }
        public bool CanBeRestoredInternally { get; set; }
    }

    public interface IUserAssignmentFailRepository
    {
        Task<IUserAssignmentFail> GetAsync(string contractAddress);
        Task SaveAsync(IUserAssignmentFail transferContract);
    }
}
