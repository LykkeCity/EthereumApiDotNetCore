namespace Lykke.Service.EthereumCore.BusinessModels
{
    public class AddressTransaction
    {
        public string Address { get; set; }
        public int Start { get; set; }
        public int Count { get; set; }
    }

    public class TokenTransaction : AddressTransaction
    {
        public string TokenAddress { get; set; }
    }
}
