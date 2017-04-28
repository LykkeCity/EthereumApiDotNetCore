using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using System.Text;
using System.IO;

namespace AzureRepositories.Repositories
{
    public class EthereumContractRepository : IEthereumContractRepository
    {
        private readonly string _blobName;
        private readonly IBlobStorage _blobStorage;

        public EthereumContractRepository(string blobName, IBlobStorage blobStorage)
        {
            _blobName = blobName;
            _blobStorage = blobStorage;
        }

        public async Task<IEthereumContract> GetAsync(string contractAddress)
        {
            var stream = await _blobStorage.GetAsync(_blobName, contractAddress);
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                string result = await reader.ReadToEndAsync();
                IEthereumContract contract = Newtonsoft.Json.JsonConvert.DeserializeObject<IEthereumContract>(result);

                return contract;
            }
        }

        public async Task SaveAsync(IEthereumContract contract)
        {
            string serialized = Newtonsoft.Json.JsonConvert.SerializeObject(contract);
            byte[] byteArray = Encoding.UTF8.GetBytes(serialized);

            await _blobStorage.SaveBlobAsync(_blobName, contract.ContractAddress, byteArray);
        }
    }
}
