using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories
{

    public interface ICoin
    {
        string Blockchain { get; }
        string Id { get; }
        string Name { get; }
        string AdapterAddress { get; set; }
        string ExternalTokenAddress { get; set; }
        int Multiplier { get; }
        bool BlockchainDepositEnabled { get; }
        bool ContainsEth { get; set; }
    }

    public class Coin : ICoin
    {
        public string Blockchain { get; set; }
        public string Id { get; set; }
        public string AdapterAddress { get; set; }
        public int Multiplier { get; set; }
        public bool BlockchainDepositEnabled { get; set; }
        public string Name { get; set; }
        public string ExternalTokenAddress { get; set; }
        public bool ContainsEth { get; set; }
    }

    public interface ICoinRepository
    {
        Task ProcessAllAsync(Func<IEnumerable<ICoin>, Task> processAction);
        Task<ICoin> GetCoin(string id);
        Task InsertOrReplace(ICoin coin);
        Task<ICoin> GetCoinByAddress(string coinAddress);
        Task<IEnumerable<ICoin>> GetAll();
    }
}
