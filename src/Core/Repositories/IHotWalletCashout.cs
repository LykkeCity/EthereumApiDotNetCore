using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IHotWalletCashout
    {
        string OperationId { get; set; }
        string FromAddress { get; set; }
        string ToAddress { get; set; }
        BigInteger Amount { get; set; }
        string TokenAddress { get; set; }
    }

    public class HotWalletCashout : IHotWalletCashout
    {
        public string OperationId { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress   { get; set; }
        public BigInteger Amount   { get; set; }
        public string TokenAddress { get; set; }
    }

    public interface IHotWalletCashoutRepository
    {
        Task<IEnumerable<IHotWalletCashout>> GetAllAsync();
        Task SaveAsync(IHotWalletCashout owner);
        Task<IHotWalletCashout> GetAsync(string operationId);
    }
}
