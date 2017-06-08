using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    public class TransactionResponse
    {
        public string TransactionHash { get; set; }
    }

    public class OperationIdResponse
    {
        public string OperationId { get; set; }
    }
}
