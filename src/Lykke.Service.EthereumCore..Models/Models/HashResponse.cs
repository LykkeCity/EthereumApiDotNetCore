using System;

namespace Lykke.Service.EthereumCore.Models
{
    public class HashResponse
    {
        public string HashHex { get; set; }
    }

    public class HashResponseWithId : HashResponse
    {
        public Guid OperationId { get; set; }
    }
}
