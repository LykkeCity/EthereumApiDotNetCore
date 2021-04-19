using System;

namespace Lykke.Service.EthereumCore.BusinessModels
{
    public class TransactionModel
    {
        public int TransactionIndex { get; set; }

        public ulong BlockNumber { get; set; }

        public string Gas { get; set; }

        public string GasPrice { get; set; }

        public string Value { get; set; }

        public string Nonce { get; set; }

        public string TransactionHash { get; set; }

        public string BlockHash { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public string Input { get; set; }

        public uint BlockTimestamp { get; set; }

        public string ContractAddress { get; set; }

        public string GasUsed { get; set; }

        public DateTime BlockTimeUtc { get; set; }

        public bool HasError { get; set; }
    }
}
