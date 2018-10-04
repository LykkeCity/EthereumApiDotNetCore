using MessagePack;

namespace Lykke.Job.EthereumCore.Contracts.Cqrs.Events
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class Erc223DepositAssignedToUserEvent
    {
        public string UserAddress { get; set; }

        public string ContractAddress { get; set; }
    }
}
