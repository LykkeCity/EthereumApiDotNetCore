using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.EthereumCore.Core.Common
{
    public class IdCheckResult
    {
        public bool IsFree { get; set; }
        public Guid ProposedId { get; set; }
    }
}
