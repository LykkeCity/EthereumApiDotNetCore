namespace Lykke.Service.EthereumCore.Core
{
    public static class QueueHelper
    {
        public static string GenerateQueueNameForContractPool(string adapterAddress)
        {
            string coinPoolQueueName = $"{Constants.ContractPoolQueuePrefix}-{adapterAddress}";

            return coinPoolQueueName;
        }
    }
}
