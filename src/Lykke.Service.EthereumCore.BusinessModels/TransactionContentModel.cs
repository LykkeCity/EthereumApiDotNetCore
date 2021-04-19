using System.Collections.Generic;

namespace Lykke.Service.EthereumCore.BusinessModels
{
    public class TransactionContentModel
    {
        public TransactionModel Transaction { get; set; }
        public List<ErcAddressHistoryModel> ErcTransfer { get; set; }
    }
}
