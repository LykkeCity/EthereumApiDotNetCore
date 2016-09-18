using System;
using Newtonsoft.Json;

namespace Core.Utils
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

		public static string ToJson(this object src, bool ignoreNulls = false)
		{
			return JsonConvert.SerializeObject(src,
				new JsonSerializerSettings { NullValueHandling = ignoreNulls ? NullValueHandling.Ignore : NullValueHandling.Include });
		}
	}
}
