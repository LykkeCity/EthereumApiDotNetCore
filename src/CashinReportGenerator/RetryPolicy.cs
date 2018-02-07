using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ReportGenerator
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
    }
}
