using Core.Repositories;
using Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Services.New.Models
{
    public class OperationHashMatchMessage : QueueMessageBase, IOperationToHashMatch
    {
        public string OperationId { get; set; }
        public string TransactionHash { get; set; }
    }
}
