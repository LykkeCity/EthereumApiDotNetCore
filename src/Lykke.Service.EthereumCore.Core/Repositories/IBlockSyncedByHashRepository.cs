using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface IBlockSyncedByHash
    {
        string Partition { get; set; }
        string BlockHash { get; set; }
        string BlockNumber { get; set; }
    }

    public class BlockSyncedByHash : IBlockSyncedByHash
    {
        public string Partition { get; set; }
        public string BlockHash { get; set; }
        public string BlockNumber { get; set; }
    }


    public interface IBlockSyncedByHashRepository
    {
        Task InsertAsync(IBlockSyncedByHash block);
        Task<IBlockSyncedByHash> GetByPartitionAndHashAsync(string partition, string hash);
        Task DeleteByPartitionAndHashAsync(string partition, string hash);
        Task<IBlockSyncedByHash> GetLastSyncedAsync(string partition);
    }
}
