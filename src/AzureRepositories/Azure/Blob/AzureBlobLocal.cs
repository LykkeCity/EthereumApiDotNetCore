using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Core.Utils;

namespace AzureRepositories.Azure.Blob
{
    public class AzureBlobLocal : IBlobStorage
    {
        private readonly string _host;

        public AzureBlobLocal(string host)
        {
            _host = host;
        }

        public Stream this[string container, string key] => GetAsync(container, key).Result;

        public Task SaveBlobAsync(string container, string key, Stream bloblStream)
        {
            return PostHttpReqest(container, key, bloblStream.ToBytes());
        }

        public Task SaveBlobAsync(string container, string key, byte[] blob)
        {
            return PostHttpReqest(container, key, blob);
        }

        public Task<bool> HasBlobAsync(string container, string key)
        {
            throw new NotImplementedException();
        }

        public Task<DateTime> GetBlobsLastModifiedAsync(string container)
        {
            return Task.Run(() => DateTime.UtcNow);
        }

        public async Task<Stream> GetAsync(string blobContainer, string key)
        {
            return await GetHttpReqestAsync(blobContainer, key);
        }

        public Task<string> GetAsTextAsync(string blobContainer, string key)
        {
            throw new NotImplementedException();
        }

        public string GetBlobUrl(string container, string key)
        {
            return string.Empty;
        }

        public Task<IEnumerable<string>> FindNamesByPrefixAsync(string container, string prefix)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetListOfBlobsAsync(string container)
        {
            throw new NotImplementedException();
        }

        public Task DelBlobAsync(string blobContainer, string key)
        {
            throw new NotImplementedException();
        }

        private string CompileRequestString(string container, string id)
        {
            return _host + "/b/" + container + "/" + id;
        }

        private async Task<MemoryStream> GetHttpReqestAsync(string container, string id)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));


                var oWebResponse = await client.GetAsync(CompileRequestString(container, id));


                if ((int) oWebResponse.StatusCode == 201)
                    return null;

                var receiveStream = await oWebResponse.Content.ReadAsStreamAsync();

                if (receiveStream == null)
                    throw new Exception("ReceiveStream == null");

                var ms = new MemoryStream();
                receiveStream.CopyTo(ms);
                return ms;
            }
        }

        private async Task<MemoryStream> PostHttpReqest(string container, string id, byte[] data)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

                var byteContent = new ByteArrayContent(data);

                var oWebResponse = await client.PostAsync(CompileRequestString(container, id), byteContent);
                var receiveStream = await oWebResponse.Content.ReadAsStreamAsync();

                if (receiveStream == null)
                    throw new Exception("ReceiveStream == null");

                var ms = new MemoryStream();
                receiveStream.CopyTo(ms);
                return ms;
            }
        }


        public void SaveBlob(string container, string key, Stream bloblStream)
        {
            SaveBlobAsync(container, key, bloblStream).Wait();
        }

        public void DelBlob(string container, string key)
        {
            throw new NotImplementedException();
        }
    }
}