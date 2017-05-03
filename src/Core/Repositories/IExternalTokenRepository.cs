using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IExternalToken
    {
        string Id { get; set; }
        string Name { get; set; }
        string ContractAddress { get; set; }
    }

    public class ExternalToken : IExternalToken
    {
       public string Id { get; set; }
       public string Name { get; set; }
       public string ContractAddress { get; set; }
    }

    public interface IExternalTokenRepository
    {
        Task SaveAsync(IExternalToken transferContract);
        Task<IExternalToken> GetAsync(string externalTokenAddress);
    }
}
