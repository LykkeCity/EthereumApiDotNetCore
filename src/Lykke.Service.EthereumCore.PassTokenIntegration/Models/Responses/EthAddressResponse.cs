using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Lykke.Service.EthereumCore.PassTokenIntegration.Models.Requests
{
    [DataContract]
    public class EthAddressResponse
    {
        [DataMember(Name = "status")]
        public string Status { get; set; }

        [DataMember(Name = "data")]
        public EthAddressData Data { get; set; }
    }

    [DataContract]
    public class EthAddressData
    {
        [DataMember(Name = "ticketId")]
        public string TicketId { get; set; }

        [DataMember(Name = "address")]
        public string Address { get; set; }

        [DataMember(Name = "addressType")]
        public string AddressType { get; set; }
    }
}
