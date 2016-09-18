using System;
using System.Threading.Tasks;
using Core.Log;

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

		public async Task WriteInfo(string component, string process, string context, string info, DateTime? dateTime = null)
		{
			await _consoleLog.WriteInfo(component, process, context, info);
			await _tableLog.WriteInfo(component, process, context, info);
		}

		public async Task WriteWarning(string component, string process, string context, string info, DateTime? dateTime = null)
		{
			await _consoleLog.WriteWarning(component, process, context, info);
			await _tableLog.WriteWarning(component, process, context, info);
		}

		public async Task WriteError(string component, string process, string context, Exception exeption, DateTime? dateTime = null)
		{
			await _consoleLog.WriteError(component, process, context, exeption);
			await _tableLog.WriteError(component, process, context, exeption);
		}

		public async Task WriteFatalError(string component, string process, string context, Exception exeption, DateTime? dateTime = null)
		{
			await _consoleLog.WriteFatalError(component, process, context, exeption);
			await _tableLog.WriteFatalError(component, process, context, exeption);
		}
	}
}
