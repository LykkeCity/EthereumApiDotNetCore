using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories
{

    public interface ICoinEvent
    {
        CoinEventType CoinEventType { get; set; }
        string TransactionHash { get; }
        string ContractAddress { get; }
        string FromAddress { get; }
        string ToAddress { get; }
        string Amount { get; }
        string Additional { get; }
        DateTime EventTime { get; }
        bool Success { get; set; }
    }

    public enum CoinEventType
    {
        CashinStarted,
        CashinCompleted,
        CashoutStarted,
        CashoutCompleted,
        TransferStarted,
        TransferCompleted
    }

    public class CoinEvent : ICoinEvent
    {
        public CoinEventType CoinEventType { get; set; }
        public string TransactionHash { get; private set; }
        public string ContractAddress { get; private set; }
        public string FromAddress { get; private set; }
        public string ToAddress { get; private set; }
        public string Amount { get; private set; }
        public string Additional { get; private set; }
        public DateTime EventTime { get; private set; }
        public bool Success { get; set; }

        public CoinEvent(string transactionHash, string fromAddress, string toAddress, string amount, CoinEventType coinEventType,
            string contractAddress = "", bool success = true, string additional = "")
        {
            TransactionHash = transactionHash;
            FromAddress = fromAddress;
            ToAddress = toAddress;
            Amount = amount;
            CoinEventType = coinEventType;
            ContractAddress = contractAddress;
            Success = success;
            Additional = additional;
            EventTime = DateTime.UtcNow;
        }
    }

    public interface ICoinEventRepository
    {
        Task<ICoinEvent> GetCoinEvent(string transactonHash);
        Task InsertOrReplace(ICoinEvent coinEvent);
    }
}
