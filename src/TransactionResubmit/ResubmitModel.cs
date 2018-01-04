using EthereumApi.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionResubmit
{
    public class ResubmitTransactionModel : TransferWithChangeModel
    {
        public string OperationType { get; set; }
    }

    public class ResubmitModel
    {
        public List<ResubmitTransactionModel> Transactions { get; set; }
    }
}
