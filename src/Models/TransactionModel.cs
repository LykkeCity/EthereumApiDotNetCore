using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessModels
{
    public class TransactionModel
    {
        public int TransactionIndex { get; set; }

        public long BlockNumber { get; set; }

        public string Gas { get; set; }

        public string GasPrice { get; set; }

        public string Value { get; set; }

        public string Nonce { get; set; }

        public string TransactionHash { get; set; }

        public string BlockHash { get; set; }

        public string FromProperty { get; set; }

        public string To { get; set; }

        public string Input { get; set; }

        public int BlockTimestamp { get; set; }

        public string ContractAddress { get; set; }

        public string GasUsed { get; set; }
    }
}
