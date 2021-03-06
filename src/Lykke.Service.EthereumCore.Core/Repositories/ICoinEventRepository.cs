﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{

    public interface ICoinEvent
    {
        string OperationId { get; }
        CoinEventType CoinEventType { get; set; }
        string TransactionHash { get; set; }
        string ContractAddress { get; }
        string FromAddress { get; }
        string ToAddress { get; }
        string Amount { get; set; }
        string Additional { get; }
        DateTime EventTime { get; }
        bool Success { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CoinEventType
    {
        CashinStarted,
        CashinCompleted,
        CashoutStarted,
        CashoutCompleted,
        TransferStarted,
        TransferCompleted,
        CashoutFailed
    }

    public class CoinEvent : ICoinEvent
    {
        public string OperationId { get; set; }
        public CoinEventType CoinEventType { get; set; }
        public string TransactionHash { get; set; }
        public string ContractAddress { get; private set; }
        public string FromAddress { get; private set; }
        public string ToAddress { get; private set; }
        public string Amount { get; set; }
        public string Additional { get; private set; }
        public DateTime EventTime { get; private set; }
        public bool Success { get; set; }

        public CoinEvent(string operationId, string transactionHash, string fromAddress, string toAddress, string amount, CoinEventType coinEventType,
            string contractAddress = "", bool success = true, string additional = "")
        {
            OperationId = operationId;
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
        Task<ICoinEvent> GetCoinEventById(string operationId);
        Task<IEnumerable<ICoinEvent>> GetAll();
    }
}
