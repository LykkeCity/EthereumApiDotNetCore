using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Core.Utils
{
	public static class Utils
	{
		public static IEnumerable<IEnumerable<T>> ToPieces<T>(this IEnumerable<T> src, int countInPicese)
		{
			var result = new List<T>();

			foreach (var itm in src)
			{
				result.Add(itm);
				if (result.Count >= countInPicese)
				{
					yield return result;
					result = new List<T>();
				}
			}

			if (result.Count > 0)
				yield return result;
		}

		public static byte[] ToBytes(this Stream src)
		{
			var memoryStream = src as MemoryStream;

			if (memoryStream != null)
				return memoryStream.ToArray();


			src.Position = 0;
			var result = new MemoryStream();

			src.CopyTo(result);
			return result.ToArray();
		}

		public static MemoryStream ToStream(this string src)
		{
			return new MemoryStream(Encoding.UTF8.GetBytes(src))
			{
				Position = 0
			};
		}

		public static Stream ToStream(this byte[] src)
		{
			if (src == null)
				return null;

			return new MemoryStream(src) { Position = 0 };
		}

		public static T ParseEnum<T>(this string value)
		{
			return (T)Enum.Parse(typeof(T), value, true);
		}


		public static T ParseEnum<T>(this string value, T defaultValue)
		{
			try
			{
				return (T)Enum.Parse(typeof(T), value, true);
			}
			catch (Exception)
			{
				return defaultValue;
			}
		}
	}
}
