using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Job.EthereumCore.Contracts.Cqrs.Events
{
    public class Erc223DepositAssignedToUserEvent
    {
        public string UserAddress { get; set; }

        public string ContractAddress { get; set; }
    }
}
