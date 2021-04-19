using System;

namespace Lykke.Service.EthereumCore.Core.Common
{
    public class IdCheckResult
    {
        public bool IsFree { get; set; }
        public Guid ProposedId { get; set; }
    }
}
