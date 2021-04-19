using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface IExternalToken
    {
        string Id { get; set; }
        string Name { get; set; }
        string ContractAddress { get; set; }
        byte Divisibility { get; set; }
        string TokenSymbol { get; set; }
        string Version { get; set; }
        string InitialSupply { get; set; }
    }

    public class ExternalToken : IExternalToken
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ContractAddress { get; set; }
        public byte Divisibility { get; set; }
        public string TokenSymbol { get; set; }
        public string Version { get; set; }
        public string InitialSupply { get; set; }
    }

    public interface IExternalTokenRepository
    {
        Task SaveAsync(IExternalToken transferContract);
        Task<IExternalToken> GetAsync(string externalTokenAddress);
        Task<IEnumerable<IExternalToken>> GetAllAsync();
    }
}
