﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface IUserTransferWallet
    {
        string UserAddress { get; }
        string TransferContractAddress { get; }
        DateTime UpdateDate { get; }
        string LastBalance { get; set; }
    }

    public class UserTransferWallet : IUserTransferWallet
    {
        public string UserAddress { get; set; }
        public string TransferContractAddress { get; set; }
        public DateTime UpdateDate { get; set; }
        public string LastBalance { get; set; }
    }

    public interface IUserTransferWalletRepository
    {
        Task SaveAsync(IUserTransferWallet wallet);
        Task ReplaceAsync(IUserTransferWallet wallet);
        Task DeleteAsync(string userAddress, string transferContractAddress);
        Task<IUserTransferWallet> GetUserContractAsync(string userAddress, string transferContractAddress);
        string FormatAddressForErc20(string depositContractAddress, string erc20TokenAddress);
        Task<IEnumerable<IUserTransferWallet>> GetAllAsync();
    }
}
