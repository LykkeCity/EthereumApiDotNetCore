namespace Lykke.Service.EthereumCore.Services.Coins.Models
{
	public class CoinTransactionCompleteEvent
	{
		public string TransactionHash { get; set; }

		public int ConfirmationLevel { get; set; }

		public bool Error { get; set; }
	}
}
