using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.EthereumCore.BusinessModels
{
    public class TransactionContentModel
    {
        public TransactionModel Transaction { get; set; }
        public List<ErcAddressHistoryModel> ErcTransfer { get; set; }
    }
}
