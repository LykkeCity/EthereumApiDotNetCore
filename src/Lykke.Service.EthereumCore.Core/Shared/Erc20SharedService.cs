using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Core.Settings;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;

namespace Lykke.Service.EthereumCore.Core.Shared
{
    public static class Erc20SharedService
    {
        public const string TransferAllTokensFuncName = "transferAllTokens";

        public static async Task<string> StartDepositTransferAsync(IWeb3 web3, 
            BaseSettings settings,
            string fromAddress, 
            string depositContractAddress, 
            string erc20TokenAddress, 
            string destinationAddress,
            ILog logger)
        {
            Contract contract = web3.Eth.GetContract(settings.Erc20DepositContract.Abi, depositContractAddress);
            var cashin = contract.GetFunction(TransferAllTokensFuncName);
            
            logger.Info("Starting deposit transfer of ERC20 tokens ", new
            {
                settings.GasForHotWalletTransaction,
                TokenAddress = erc20TokenAddress,
                DestinationAddress = destinationAddress
            });
            
            var cashinWouldBeSuccesfull = await cashin.CallAsync<bool>(fromAddress,
                new HexBigInteger(settings.GasForHotWalletTransaction), 
                new HexBigInteger(0),
                erc20TokenAddress,
                destinationAddress);

            if (!cashinWouldBeSuccesfull)
            {
                throw new ClientSideException(ExceptionType.WrongParams, 
                    $"CAN'T Estimate Cashin {depositContractAddress}, {erc20TokenAddress}, {destinationAddress}");
            }

            string trHash = await cashin.SendTransactionAsync(fromAddress,
                new HexBigInteger(settings.GasForHotWalletTransaction), 
                new HexBigInteger(0), 
                erc20TokenAddress,
                destinationAddress);

            return trHash;
        }

        public static async Task<bool> EstimateDepositTransferAsync(IWeb3 web3,
            string settingsErc20DepositContractAbi,
            string fromAddress, 
            string depositContractAddress, 
            string erc20TokenAddress, 
            string destinationAddress,
            IBaseSettings settings,
            ILog logger)
        {
            logger.Info("Estimation of deposit transfer of ERC20 tokens", new
            {
                settings.GasForHotWalletTransaction,
                TokenAddress = erc20TokenAddress,
                DestinationAddress = destinationAddress
            });
            
            Contract contract = web3.Eth.GetContract(settingsErc20DepositContractAbi, depositContractAddress);
            var cashin = contract.GetFunction(TransferAllTokensFuncName);

            var cashinWouldBeSuccesfull = await cashin.CallAsync<bool>(fromAddress,
                new HexBigInteger(settings.GasForHotWalletTransaction),
                new HexBigInteger(0),
                erc20TokenAddress,
                destinationAddress);

            return cashinWouldBeSuccesfull;
        }
    }
}
