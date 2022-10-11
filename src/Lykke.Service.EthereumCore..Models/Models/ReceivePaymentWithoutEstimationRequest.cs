namespace Lykke.Service.EthereumCoreSelfHosted.Models
{
    public class ReceivePaymentWithoutEstimationRequest
    {
        public string DepositContractAddress { get; set; }
        public string Erc20TokenContractAddress { get; set; }
        public string ToAddress { get; set; }
        public int? Gas { get; set; }
    }
}