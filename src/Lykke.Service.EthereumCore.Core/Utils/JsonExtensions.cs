using System;
using Newtonsoft.Json;

namespace Lykke.Service.EthereumCore.Core.Utils
{
	public static class JsonExtensions
	{
		public static T DeserializeJson<T>(this string json, Func<T> createDefault)
		{
			try
			{
				return JsonConvert.DeserializeObject<T>(json);
			}
			catch (Exception)
			{
				return createDefault();
			}
		}

		public static T DeserializeJson<T>(this string json)
		{
			return JsonConvert.DeserializeObject<T>(json);
		}
	}
}
