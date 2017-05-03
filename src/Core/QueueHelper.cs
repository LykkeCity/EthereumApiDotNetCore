using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core
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
