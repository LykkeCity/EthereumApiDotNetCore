namespace Core
{
	public class Constants
	{
		/// <summary>
		/// Used to change table and queue names in testing enviroment
		/// </summary>
		public static string StoragePrefix { get; set; } = "";		

		public const string EthereumContractQueue = "ethereum-contract-queue";
		public const string EthereumOutQueue = "ethereum-queue-out";
		public const string EmailNotifierQueue = "emailsqueue";

		/// <summary>
		/// Used to internal monitoring of refill transactions
		/// </summary>
		public const string ContractTransferQueue = "ethereum-contract-transfer-queue";

		/// <summary>
		/// Used to internal monitoring of coin transactions
		/// </summary>
		public const string TransactionMonitoringQueue = "ethereum-transaction-monitor-queue";

		/// <summary>
		/// Used to notify external services about coin transactions
		/// </summary>
		public const string CoinTransactionQueue = "ethereum-coin-transaction-queue";

		/// <summary>
		/// Used to notify external services about events in coin contracts
		/// </summary>
		public const string CoinEventQueue = "ethereum-coin-event-queue";
		
		//table names
		public const string MonitoringTable = "Monitoring";
		public const string UserContractsTable = "UserContracts";
		public const string AppSettingsTable = "AppSettings";
		public const string TransactionsTable = "Transactions";
		public const string CoinFiltersTable = "CoinFilters";


		public const int GasForUserContractTransafer = 50000;
		public const int GasForCoinTransaction = 200000;

		
		// app	settings keys
		public const string EthereumFilterSettingKey = "ethereum-user-contract-filter";		


		//coin contract event names
		public const string CashInEvent = "CoinCashIn";
		public const string CashOutEvent = "CoinCashOut";
		public const string TransferEvent = "CoinTransfer";

	}
}
