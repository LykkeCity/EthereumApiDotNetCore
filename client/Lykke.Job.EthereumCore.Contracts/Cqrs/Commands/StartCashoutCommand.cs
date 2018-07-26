using System;
using MessagePack;

namespace Lykke.Job.EthereumCore.Contracts.Cqrs.Commands
{
    /// <summary>
    /// Cashout command
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class StartCashoutCommand
    {
        public Guid Id { get; set; }

        public string AssetId { get; set; }

        public string FromAddress { get; set; }

        public string ToAddress { get; set; }

        public decimal Amount { get; set; }
    }
}
