using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Core.Services;
using Lykke.Service.EthereumCore.Core.Settings;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using System.Numerics;

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
            string destinationAddress)
        {
            Contract contract = web3.Eth.GetContract(settings.Erc20DepositContract.Abi, depositContractAddress);
            var cashin = contract.GetFunction(TransferAllTokensFuncName);
            var cashinWouldBeSuccesfull = await cashin.CallAsync<bool>(fromAddress,
                new HexBigInteger(Constants.GasForHotWalletTransaction), 
                new HexBigInteger(0),
                erc20TokenAddress,
                destinationAddress);

            if (!cashinWouldBeSuccesfull)
            {
                throw new ClientSideException(ExceptionType.WrongParams, 
                    $"CAN'T Estimate Cashin {depositContractAddress}, {erc20TokenAddress}, {destinationAddress}");
            }

            string trHash = await cashin.SendTransactionAsync(fromAddress,
                new HexBigInteger(Constants.GasForHotWalletTransaction), 
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
            string destinationAddress)
        {
            Contract contract = web3.Eth.GetContract(settingsErc20DepositContractAbi, depositContractAddress);
            var cashin = contract.GetFunction(TransferAllTokensFuncName);

            var cashinWouldBeSuccesfull = await cashin.CallAsync<bool>(fromAddress,
                new HexBigInteger(Constants.GasForHotWalletTransaction),
                new HexBigInteger(0),
                erc20TokenAddress,
                destinationAddress);

            return cashinWouldBeSuccesfull;
        }
    }
}
