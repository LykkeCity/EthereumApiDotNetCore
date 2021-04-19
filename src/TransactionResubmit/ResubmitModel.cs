using Lykke.Service.EthereumCore.Client.Models;
using System.Collections.Generic;

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
