namespace Lykke.Service.EthereumCore.Services.Coins.Models
{
    public class CoinContractPublicEvent
    {
        public string CoinName { get; set; }

		public string Address { get; set; }

		public string EventName { get; set; }

		public string Caller { get; set; }

		public string From { get; set; }

		public string To { get; set; }

		public decimal Amount { get; set; }
    }
}
