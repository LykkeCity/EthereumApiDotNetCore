using Common.Log;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace EthereumApi.Utils
{
    public static class TimeoutPolicy
    {
        public static CancellationToken GetCancellationTokenForContractCreation(ILog log)
        {
            CancellationTokenSource tkSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            return tkSource.Token;
        }
    }
}
