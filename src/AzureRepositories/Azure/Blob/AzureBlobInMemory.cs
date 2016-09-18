using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Utils;

namespace AzureRepositories.Azure.Blob
{
	internal static class BlobInMemoryHelper
	{
		public static void AddOrReplace(this Dictionary<string, byte[]> blob, string key, byte[] data)
		{
			if (blob.ContainsKey(key))
			{
				blob[key] = data;
				return;
			}


			blob.Add(key, data);
		}

		public static byte[] GetOrNull(this Dictionary<string, byte[]> blob, string key)
		{
			if (blob.ContainsKey(key))
				return blob[key];


			return null;
		}
	}


	public class AzureBlobInMemory : IBlobStorage
	{
		private readonly Dictionary<string, Dictionary<string, byte[]>> _blobs =
			new Dictionary<string, Dictionary<string, byte[]>>();


		private readonly object _lockObject = new object();


		private Dictionary<string, byte[]> GetBlob(string container)
		{
			if (!_blobs.ContainsKey(container))
				_blobs.Add(container, new Dictionary<string, byte[]>());


			return _blobs[container];
		}

		public void SaveBlob(string container, string key, Stream bloblStream)
		{
			lock (_lockObject)
				GetBlob(container).AddOrReplace(key, bloblStream.ToBytes());
		}

		public Task SaveBlobAsync(string container, string key, Stream bloblStream)
		{
			SaveBlob(container, key, bloblStream);
			return Task.FromResult(0);
		}

		public Task SaveBlobAsync(string container, string key, byte[] blob)
		{
			lock (_lockObject)
				GetBlob(container).AddOrReplace(key, blob);
			return Task.FromResult(0);
		}

		public Task<bool> HasBlobAsync(string container, string key)
		{
			lock (_lockObject)
				return Task.Run(() => _blobs[container].ContainsKey(key));
		}

		public Task<DateTime> GetBlobsLastModifiedAsync(string container)
		{
			return Task.Run(() => DateTime.UtcNow);
		}

		public Stream this[string container, string key]
		{
			get
			{
				lock (_lockObject)
					return GetBlob(container).GetOrNull(key).ToStream();
			}
		}

		public Task<Stream> GetAsync(string container, string key)
		{
			var result = this[container, key];
			return Task.FromResult(result);
		}

		public Task<string> GetAsTextAsync(string blobContainer, string key)
		{
			var result = this[blobContainer, key];
			using (var sr = new StreamReader(result))
			{
				return Task.FromResult(sr.ReadToEnd());
			}
		}

		public string GetBlobUrl(string container, string key)
		{
			return string.Empty;
		}

		public Task<IEnumerable<string>> FindNamesByPrefixAsync(string container, string prefix)
		{
			lock (_lockObject)
				return Task.Run(() => GetBlob(container).Where(itm => itm.Key.StartsWith(prefix)).Select(itm => itm.Key));
		}

		public Task<IEnumerable<string>> GetListOfBlobsAsync(string container)
		{
			lock (_lockObject)
				return Task.Run(() => GetBlob(container).Select(itm => itm.Key));
		}

		public void DelBlob(string container, string key)
		{
			lock (_lockObject)
				GetBlob(container).Remove(key);
		}

		public Task DelBlobAsync(string blobContainer, string key)
		{
			DelBlob(blobContainer, key);
			return Task.FromResult(0);
		}
	}
}
