﻿using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface IBlockSynced
    {
        string CoinAdapterAddress { get; set; }
        string BlockNumber { get; set; }
    }

    public class BlockSynced : IBlockSynced
    {
        public string CoinAdapterAddress { get; set; }

        public string BlockNumber { get; set; }
    }


    public interface IBlockSyncedRepository
    {
        Task ClearForAdapter(string coinAdapterAddress);
        Task InsertAsync(IBlockSynced block);
        Task<IBlockSynced> GetLastSyncedAsync(string coinAdapter);
    }
}
