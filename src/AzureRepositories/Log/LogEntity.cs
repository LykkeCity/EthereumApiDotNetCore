using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Log
{
	public class LogEntity : TableEntity
	{
		public static string GeneratePartitionKey(DateTime dateTime)
		{
			return dateTime.ToString("yyyy-MM-dd");
		}

		public DateTime DateTime { get; set; }
		public string Level { get; set; }
		public string Component { get; set; }
		public string Process { get; set; }
		public string Context { get; set; }
		public string Type { get; set; }
		public string Stack { get; set; }
		public string Msg { get; set; }

		public static LogEntity Create(string level, string component, string process, string context, string type, string stack, string msg, DateTime dateTime)
		{
			return new LogEntity
			{
				PartitionKey = GeneratePartitionKey(dateTime),
				DateTime = dateTime,
				Level = level,
				Component = component,
				Process = process,
				Context = context,
				Type = type,
				Stack = stack,
				Msg = msg
			};
		}

	}
}
