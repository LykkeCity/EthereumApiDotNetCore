﻿using Lykke.Service.EthereumCore.Core.Exceptions;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using System.Numerics;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.EthereumCore.Core.Settings;
using Nethereum.Web3;
using Common;

namespace Lykke.Service.EthereumCore.Core.Shared
{
    public static class Erc223SharedService
    {
        public const string TransferTokensFuncName = "transferTokens";

        public static async Task<string> StartDepositTransferAsync(IWeb3 web3, 
            string settingsErc20DepositContractAbi,
            string fromAddress, 
            string depositContractAddress, 
            string erc20TokenAddress, 
            string destinationAddress,
            BigInteger tokenAmount,
            IBaseSettings settings,
            ILog logger)
        {
            Contract contract = web3.Eth.GetContract(settingsErc20DepositContractAbi, depositContractAddress);
            var cashin = contract.GetFunction(TransferTokensFuncName);

            logger.WriteInfoAsync(nameof(Erc223SharedService), nameof(StartDepositTransferAsync),
                new
                {
                    settings.GasForHotWalletTransaction,
                    TokenAddress = erc20TokenAddress,
                    DestinationAddress = destinationAddress
                }.ToJson(),
                "Starting deposit transfer of ERC223 tokens");
            
            var cashinWouldBeSuccesfull = await cashin.CallAsync<bool>(fromAddress,
                new HexBigInteger(settings.GasForHotWalletTransaction), 
                new HexBigInteger(0),
                erc20TokenAddress,
                destinationAddress,
                tokenAmount);

            if (!cashinWouldBeSuccesfull)
            {
                throw new ClientSideException(ExceptionType.WrongParams, 
                    $"CAN'T Estimate Cashin {depositContractAddress}, {erc20TokenAddress}, {destinationAddress}");
            }

            string trHash = await cashin.SendTransactionAsync(fromAddress,
                new HexBigInteger(settings.GasForHotWalletTransaction), 
                new HexBigInteger(0),
                erc20TokenAddress,
                destinationAddress,
                tokenAmount);

            return trHash;
        }

        public static async Task<bool> EstimateDepositTransferAsync(IWeb3 web3,
            string settingsErc20DepositContractAbi,
            string fromAddress, 
            string depositContractAddress, 
            string erc20TokenAddress, 
            string destinationAddress,
            BigInteger tokenAmount,
            IBaseSettings settings,
            ILog logger)
        {
            logger.WriteInfoAsync(nameof(Erc223SharedService), nameof(EstimateDepositTransferAsync),
                new
                {
                    settings.GasForHotWalletTransaction,
                    TokenAddress = erc20TokenAddress,
                    DestinationAddress = destinationAddress
                }.ToJson(),
                "Estimation of deposit transfer of ERC223 tokens");
            
            Contract contract = web3.Eth.GetContract(settingsErc20DepositContractAbi, depositContractAddress);
            var cashin = contract.GetFunction(TransferTokensFuncName);

            var cashinWouldBeSuccesfull = await cashin.CallAsync<bool>(fromAddress,
                new HexBigInteger(settings.GasForHotWalletTransaction),
                new HexBigInteger(0),
                erc20TokenAddress,
                destinationAddress,
                tokenAmount);

            return cashinWouldBeSuccesfull;
        }
    }
}
