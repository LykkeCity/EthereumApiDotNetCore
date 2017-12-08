using System.Numerics;

namespace Core
{
    public class Constants
    {
        public const string BigIntTemplate =  "^[1-9][0-9]*$";
        public const string BigIntAllowZeroTemplate = "^([1-9][0-9])|0*$";
        /// <summary>
        /// Used to change table and queue names in testing enviroment
        /// </summary>
        public static string StoragePrefix { get; set; } = "";
        public const string AddressForRoundRobinTransactionSending = "0xFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
        public const string EmptyEthereumAddress = "0x0000000000000000000000000000000000000000";
        public const string EventTraceTable = "EventTrace";
        public const string TransferContractUserAssignmentQueueName = "transfer-contract-user-assignment";
        public const string SlackNotifierQueue = "slack-notifications";
        public const string EthereumContractQueue = "ethereum-contract-queue";
        public const string EthereumOutQueue = "ethereum-queue-out";
        public const string EmailNotifierQueue = "emailsqueue";
        public const string ContractPoolQueuePrefix = "ethereum-tc-pool";
        public const string CoinEventResubmittQueue = "ethereum-coin-event-resubmitt-queue";
        public const string CashinCompletedEventsQueue = "cashin-completed-events-queue";
        public const string HotWalletCashoutQueue = "hotwallet-cashout-queue";
        public const string HotWalletTransactionMonitoringQueue = "hotwallet-transaction-monitoring-queue";
        public const string Erc20DepositContractPoolQueue = "erc20-deposit-contract-pool-queue";
        public const string Erc20DepositCashinTransferQueue = "erc20-deposit-cashin-transfer-queue";
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

        /// <summary>
        /// Used to process manual payments
        /// </summary>
        public const string UserContractManualQueue = "ethereum-user-payment-manual";

        //table names
        public const string MonitoringTable = "Monitoring";
        public const string UserContractsTable = "UserContracts";
        public const string AppSettingsTable = "AppSettings";
        public const string TransactionsTable = "Transactions";
        public const string CoinFiltersTable = "CoinFilters";
        public const string CoinTable = "CoinTable";
        public const string CoinTableInedex = "CoinTableIndex";
        public const string HotWalletCashoutTable = "HotWalletCashout";
        public const string HotWalletCashoutTransactionTable = "HotWalletCashoutTransaction";
        public const string Erc20DepositContractTable = "Erc20DepositContracts";

        public const int GasForUserContractTransafer = 50000;
        public const int GasForCoinTransaction = 200000;
        public const int GasForEthCashin = 800000;
        public const int HalfGasLimit = 100000;


        // app	settings keys
        public const string EthereumFilterSettingKey = "ethereum-user-contract-filter";

        // user payment event
        public const string UserPaymentEvent = "PaymentFromUser";

        //coin contract event names
        public const string CashInEvent = "CoinCashIn";
        public const string CashOutEvent = "CoinCashOut";
        public const string TransferEvent = "CoinTransfer";

        public const string EthereumBlockchain = "Ethereum";
        public const string TransferContractTable = "TransferContract";
        public const string EthereumContractsBlob = "EthereumContracts";
        public const string UserTransferWalletTable = "UserTransferWallet";
        public const string ExternalTokenTable = "ExternalToken";
        public const string CoinEventEntityTable = "CoinEventEntity";
        public const string UserPaymentHistoryTable = "UserPaymentHistory";
        public const string PendingTransactions = "PendingTransactions";
        public const string PendingOperationsTable = "PendingOperation";
        public const string OperationToHashMatchTable = "OperationToHashMatch";
        public const string BlockSyncedTable = "BlockSynced";
        public const string CashInEventTable = "CashInEvent";
        public const string PendingOperationsQueue = "pending-operations";
        public const string NonceCacheTable = "NonceCache";
        public const string UserAssignmentFailTable = "UserAssignmentFail";
        public const string OperationResubmittTable = "OperationResubmitt";
        public const string GasPriceTable = "GasPrice";

        public const string OwnerTable = "Owner";
        public static BigInteger DefaultTransactionGas = 21000;
        public const string Erc20TransferSignature =  "0xa9059cbb";
    }

    public static class OperationTypes
    {
        public const string Transfer = "Transfer";
        public const string Cashout = "Cashout";
        public const string TransferWithChange = "TransferWithChange";
    }
}
