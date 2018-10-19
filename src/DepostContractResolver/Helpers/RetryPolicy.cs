using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Log;

namespace DepositContractResolver.Helpers
{
    public static class RetryPolicy
    {
        /// <summary>
        /// Retry policy with exponential waiting before retries
        /// </summary>
        /// <returns></returns>
        public static async Task ExecuteAsync(Func<Task> func, int retryCount, int delayMs)
        {
            bool isExecutionCompleted = false;
            int currentTry = 1;

            do
            {
                try
                {
                    await func();
                    isExecutionCompleted = true;
                }
                catch (Exception e)
                {
                    if (currentTry >= retryCount)
                    {
                        throw;
                    }
                    //Exponentially wait 200ms - 400ms - 800ms -...
                    var retryVariable = Math.Pow(2, currentTry);
                    await Task.Delay(delayMs * (int)retryVariable);
                    currentTry++;
                }

            } while (!isExecutionCompleted);
        }

        /// <summary>
        /// Retry policy with exponential waiting before retries
        /// </summary>
        /// <returns></returns>
        public static async Task ExecuteUnlimitedAsync(Func<Task> func, int delayMs, ILog log)
        {
            bool isExecutionCompleted = false;
            int currentTry = 1;

            do
            {
                try
                {
                    await func();
                    isExecutionCompleted = true;
                }
                catch (Exception e)
                {
                    await log.WriteErrorAsync(nameof(RetryPolicy), nameof(ExecuteUnlimitedAsync), $"Current try {currentTry}", e);
                    await Task.Delay(delayMs);
                    currentTry++;
                }

            } while (!isExecutionCompleted);
        }
    }
}
