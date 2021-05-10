﻿using System.Numerics;

namespace Lykke.Service.EthereumCore.Core
{
    public class Constants
    {
        public const string BigIntTemplate =  "^[1-9][0-9]*$";
        public const string BigIntAllowZeroTemplate = "^([1-9][0-9])|0*$";
        /// <summary>
        /// Used to change table and queue names in testing enviroment
        /// </summary>
        public static string StoragePrefix { get; set; } = "";

        public const string DefaultKey = "default";
        public const string LykkePayKey = "lykke-pay";
        public const string AirLinesKey = "airlines";

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
        /// Used to notify external Lykke.Service.EthereumCore.Services about coin transactions
        /// </summary>
        public const string CoinTransactionQueue = "ethereum-coin-transaction-queue";

        /// <summary>
        /// Used to notify external Lykke.Service.EthereumCore.Services about events in coin contracts
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
        public const string Erc223DepositContractTable = "Erc223DepositContracts";
        public const string BlackListAddressTable = "BlackListAddresses";
        public const string Erc20BlackListAddressTable = "BlackListAddresses";
        public const string WhiteListAddressesTable = "WhiteListAddresses";
        public const string AddressStatisticsTable = "AddressStatistics";
        public const string OverrideNonceTable = "OverrideNonce";
        public const string EthereumContractPoolTable = "EthereumContractPool";
        public const string EthereumCreatedContractTable = "EthereumCreatedContract";


        #region  LykkePay

        public const string LykkePayOperationsTable = "LykkePayOperations";
        public const string LykkePayErc223DepositContractTable = "LykkePayErc223DepositContracts";
        public const string LykkePayHotWalletCashoutTransactionTable = "LykkePayHotWalletCashoutTransaction";
        public const string LykkePayErc223TransferQueue = "lykke-pay-erc-transfers-queue";
        public const string LykkePayErc223TransferNotificationsQueue = "lykke-pay-erc-transfers-notifications-queue";
        public const string LykkePayTransactionMonitoringQueue = "lykke-pay-transaction-monitoring-queue";
        public const string LykkePayErc20DepositContractPoolQueue = "lykke-pay-erc20-deposit-contract-pool-queue";

        #endregion

        #region  Airlines

        public const string AirlinesOperationsTable = "AirlinesOperations";
        public const string AirlinesErc223DepositContractTable = "AirlinesErc223DepositContracts";
        public const string AirlinesHotWalletCashoutTransactionTable = "AirlinesHotWalletCashoutTransaction";
        public const string AirlinesErc223TransferQueue = "airlines-erc-transfers-queue";
        public const string AirlinesErc223TransferNotificationsQueue = "airlines-erc-transfers-notifications-queue";
        public const string AirlinesTransactionMonitoringQueue = "airlines-transaction-monitoring-queue";
        public const string AirlinesErc20DepositContractPoolQueue = "airlines-erc20-deposit-contract-pool-queue";

        #endregion


        public const int GasForUserContractTransafer = 50000;
        public const int GasForCoinTransaction = 200000;
        public const int GasForHotWalletTransaction = 400000;
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
        public const string BlockSyncedByHashTable = "BlockSyncedByHash";
        public const string CashInEventTable = "CashInEvent";
        public const string PendingOperationsQueue = "pending-operations";
        public const string NonceCacheTable = "NonceCache";
        public const string UserAssignmentFailTable = "UserAssignmentFail";
        public const string OperationResubmittTable = "OperationResubmitt";
        public const string GasPriceTable = "GasPrice";

        public const string OwnerTable = "Owner";
        public static BigInteger DefaultTransactionGas = 21000;
        public static BigInteger GasForEachDataByte = 68;
        public const string Erc20TransferSignature =  "0xa9059cbb";
    }

    public static class OperationTypes
    {
        public const string Transfer = "Transfer";
        public const string Cashout = "Cashout";
        public const string TransferWithChange = "TransferWithChange";
    }
}
