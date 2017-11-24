using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public enum HotWalletOperationType
    {
        Cashout,
        Cashin
    }

    public interface IHotWalletOperation
    {
        string OperationId { get; set; }
        string FromAddress { get; set; }
        string ToAddress { get; set; }
        BigInteger Amount { get; set; }
        string TokenAddress { get; set; }
        HotWalletOperationType OperationType { get; set; }
    }

    public class HotWalletOperation : IHotWalletOperation
    {
        public string OperationId { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress   { get; set; }
        public BigInteger Amount   { get; set; }
        public string TokenAddress { get; set; }
        public HotWalletOperationType OperationType { get; set; }
    }

    public interface IHotWalletOperationRepository
    {
        Task<IEnumerable<IHotWalletOperation>> GetAllAsync();
        Task SaveAsync(IHotWalletOperation owner);
        Task<IHotWalletOperation> GetAsync(string operationId);
    }
}
