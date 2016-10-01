namespace Core
{
	public class Constants
	{
		public static string StoragePrefix { get; set; } = "";

		public const string EthereumContractQueue = "ethereum-contract-queue";
		public const string EthereumOutQueue = "ethereum-queue-out";
		public const string EmailNotifierQueue = "emailsqueue";
		public const string ContractTransferQueue = "ethereum-contract-transfer-queue";

		public const string TransactionMonitoringQueue = "ethereum-transaction-monitor-queue";
		public const string CoinTransactionQueue = "ethereum-coin-transaction-queue";

		public const string EthereumFilterSettingKey = "ethereum-user-contract-filter";

		public const string MonitoringTable = "Monitoring";
		public const string UserContractsTable = "UserContracts";
		public const string AppSettingsTable = "AppSettings";
		public const string TransactionsTable = "Transactions";


		public const int GasForUserContractTransafer = 50000;


	}
}
