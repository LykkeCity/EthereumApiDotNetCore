using EthereumApi.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionResubmit
{
    public class RabbitResubmit
    {
        public string TransactionHash { get; set; }
    }

    public class RabbitList
    {
        public List<RabbitResubmit> Transactions { get; set; }
    }
}
