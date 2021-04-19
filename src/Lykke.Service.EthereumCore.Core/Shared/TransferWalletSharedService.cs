using System;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Repositories;

namespace Lykke.Service.EthereumCore.Core.Shared
{
    public static class TransferWalletSharedService
    {
        public static async Task<IUserTransferWallet> GetUserTransferWalletAsync(IUserTransferWalletRepository userTransferWalletRepository,
            string contractAddress, string erc20TokenAddress, string userAddress)
        {
            string formattedAddress =
                userTransferWalletRepository.FormatAddressForErc20(contractAddress, erc20TokenAddress).ToLower();
            IUserTransferWallet wallet =
                await userTransferWalletRepository.GetUserContractAsync(userAddress, formattedAddress);

            return wallet;
        }

        public static async Task UpdateUserTransferWalletAsync(IUserTransferWalletRepository userTransferWalletRepository,
            string contractAddress, string erc20TokenAddress, string userAddress, string balance = "")
        {
            string formattedAddress =
                userTransferWalletRepository.FormatAddressForErc20(contractAddress?.ToLower(), erc20TokenAddress?.ToLower());

            await userTransferWalletRepository.ReplaceAsync(new UserTransferWallet()
            {
                LastBalance = balance,
                TransferContractAddress = formattedAddress,
                UpdateDate = DateTime.UtcNow,
                UserAddress = userAddress
            });
        }
    }
}
