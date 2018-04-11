﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Services;
using Lykke.Service.EthereumCore.Core.Settings;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;

namespace Lykke.Service.EthereumCore.Core.Shared
{
    public static class Erc20SharedService
    {
        public static async Task<string> StartTransferAsync(IWeb3 web3, BaseSettings settings,
            string fromAddress, string depositContractAddress, string erc20TokenAddress, string destinationAddress)
        {
            Contract contract = web3.Eth.GetContract(settings.Erc20DepositContract.Abi, depositContractAddress);
            var cashin = contract.GetFunction("transferAllTokens");
            var cashinWouldBeSuccesfull = await cashin.CallAsync<bool>(fromAddress,
                new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0), erc20TokenAddress, destinationAddress);

            if (!cashinWouldBeSuccesfull)
            {
                throw new Exception($"CAN'T Estimate Cashin {depositContractAddress}, {erc20TokenAddress}, {destinationAddress}");
            }

            string trHash = await cashin.SendTransactionAsync(fromAddress,
                new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0), erc20TokenAddress, destinationAddress);

            return trHash;
        }
    }
}
