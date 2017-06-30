﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace EthereumApi.Models.Indexer
{
    [DataContract]
    public class FilteredTransactionsResponse
    {
        [DataMember(Name = "transactions")]
        public IEnumerable<TransactionResponse> Transactions { get; set; }
    }

    [DataContract]
    public class TransactionResponse
    {
        [DataMember(Name = "transactionIndex")]
        public int TransactionIndex { get; set; }
        [DataMember(Name = "blockNumber")]
        public ulong BlockNumber { get; set; }
        [DataMember(Name = "gas")]
        public string Gas { get; set; }
        [DataMember(Name = "gasPrice")]
        public string GasPrice { get; set; }
        [DataMember(Name = "value")]
        public string Value { get; set; }
        [DataMember(Name = "nonce")]
        public string Nonce { get; set; }
        [DataMember(Name = "transactionHash")]
        public string TransactionHash { get; set; }
        [DataMember(Name = "blockHash")]
        public string BlockHash { get; set; }
        [DataMember(Name = "from")]
        public string From { get; set; }
        [DataMember(Name = "to")]
        public string To { get; set; }
        [DataMember(Name = "input")]
        public string Input { get; set; }
        [DataMember(Name = "blockTimestamp")]
        public int BlockTimestamp { get; set; }
        [DataMember(Name = "contractAddress")]
        public string ContractAddress { get; set; }
        [DataMember(Name = "gasUsed")]
        public string GasUsed { get; set; }
        [DataMember(Name = "BlockTimeUtc")]
        public DateTime BlockTimeUtc { get; set; }
    }
}
