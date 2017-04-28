using System;
using System.Threading.Tasks;
using Common.Log;

namespace AzureRepositories.Log
{
    public class LogToTableAndConsole : ILog
    {
        private readonly LogToTable _tableLog;
        private readonly LogToConsole _consoleLog;

        public LogToTableAndConsole(LogToTable tableLog, LogToConsole consoleLog)
        {
            _tableLog = tableLog;
            _consoleLog = consoleLog;
        }

        public async Task WriteInfoAsync(string component, string process, string context, string info, DateTime? dateTime = null)
        {
            await _consoleLog.WriteInfoAsync(component, process, context, info);
            await _tableLog.WriteInfoAsync(component, process, context, info);
        }

        public async Task WriteWarningAsync(string component, string process, string context, string info, DateTime? dateTime = null)
        {
            await _consoleLog.WriteWarningAsync(component, process, context, info);
            await _tableLog.WriteWarningAsync(component, process, context, info);
        }

        public async Task WriteErrorAsync(string component, string process, string context, Exception exeption, DateTime? dateTime = null)
        {
            await _consoleLog.WriteErrorAsync(component, process, context, exeption);
            await _tableLog.WriteErrorAsync(component, process, context, exeption);
        }

        public async Task WriteFatalErrorAsync(string component, string process, string context, Exception exeption, DateTime? dateTime = null)
        {
            await _consoleLog.WriteFatalErrorAsync(component, process, context, exeption);
            await _tableLog.WriteFatalErrorAsync(component, process, context, exeption);
        }
    }
}
