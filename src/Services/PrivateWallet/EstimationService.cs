using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Core.PrivateWallet;
using Lykke.Service.EthereumCore.Core.Services;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Common;

namespace Lykke.Service.EthereumCore.Services.PrivateWallet
{
    /*
        1 The user wants to withdraw some eth amount << available to the ordinary wallet
        Expected {true, 21 000, null}

        2 The user wants to withdraw some eth amount << available to the contract wallet
        Expected {true, 56 321, null}

        //May be, it will be implemented later
        3 The user wants to withdraw some eth amount << available to the contract wallet but estimated gas exceeds 200 000
        Expected {false, 432 123, The transaction spends too much gas, please set up the parameters manually}

        4 The user wants to withdraw eth amount = available
        Logic
        1step try transaction with amount 1*10^accuracy to calculate amount for gas
        2step Then retry transaction witn amount = (amount - gas*gasPrice)
        Expected {true, 32 123, null}

        4.1 There is no enough funds for transaction with amount = (amount - gas*gasPrice)
        Expected {false, null, Not enough funds to pay the transaction fee}

        4.2 The gas is changed for transaction witn amount = (amount - gas*gasPrice)
        Expected {false, null, The transaction spends too much gas, please set up the parameters manually}

        5 The user wants to withdraw eth amount = available < Fee
        Expected {false, null, Not enough funds to pay the transaction fee}


        6 The user wants to withdraw erc amount = any, eth available >> fee
        Expected {true, 49 000, null}

        7 The user wants to withdraw erc amount = any, eth available < fee  (include case 0 eth)
        Expected {false, null, Not enough funds to pay the transaction fee}
     */

    public class EstimationService : IEstimationService
    {
        private readonly IWeb3 _web3;

        public EstimationService(IWeb3 web3)
        {
            _web3 = web3;
        }

        public async Task<OperationEstimationV2Result> EstimateTransactionExecutionCostAsync(
            string fromAddress,
            string toAddress,
            BigInteger amount,
            BigInteger gasPrice,
            string transactionData)
        {
            var fromAddressBalance = await _web3.Eth.GetBalance.SendRequestAsync(fromAddress, BlockParameter.CreatePending());
            //var currentGasPrice = await _web3.Eth.GasPrice.SendRequestAsync();
            var value = new HexBigInteger(amount);
            CallInput callInput;
            HexBigInteger estimatedGas;
            bool isAllowed = true;
            callInput = new CallInput(transactionData, toAddress, value);
            callInput.From = fromAddress;

            try
            {
                //Throws on wrong call
                if (await IsSmartContracAsync(toAddress))
                {
                    var callResult = await _web3.Eth.Transactions.Call.SendRequestAsync(callInput);
                    //Get amount of gas for smart contract execution
                    estimatedGas = await _web3.Eth.Transactions.EstimateGas.SendRequestAsync(callInput);
                }
                else
                {
                    estimatedGas = new HexBigInteger(Lykke.Service.EthereumCore.Core.Constants.DefaultTransactionGas);
                }

                //Recalculate transaction eth Amount
                var diff = fromAddressBalance -
                           (amount + estimatedGas.Value * gasPrice);

                if (diff < 0)
                {
                    amount += diff;
                }


                if (string.IsNullOrEmpty(transactionData))
                {
                    if (amount <= 0)
                        throw new ClientSideException(ExceptionType.NotEnoughFunds, $"Not enough Ethereum on {fromAddress}");
                }
                else if (diff < 0)
                {
                    Console.WriteLine($"Not enough funds! - fromAddress: {fromAddress}, diff: {diff}, amount: {amount}, fromAddressBalance: {fromAddressBalance}");
                    throw new ClientSideException(ExceptionType.NotEnoughFunds, $"Not enough Ethereum on {fromAddress}");
                }

                callInput.Value = new HexBigInteger(amount);
                callInput.GasPrice = new HexBigInteger(gasPrice);

                //reestimate with new arguments
                estimatedGas = await _web3.Eth.Transactions.EstimateGas.SendRequestAsync(callInput);
            }
            catch (Nethereum.JsonRpc.Client.RpcResponseException rpcException)
            {
                Console.WriteLine($"Exception: {rpcException.Message}, {rpcException.RpcError.ToJson()}");
                var rpcError = rpcException?.RpcError;
                if (rpcError != null &&
                    rpcError.Code == -32000)
                {
                    estimatedGas = new HexBigInteger(0);
                    isAllowed = false;
                }
                else
                {
                    throw new ClientSideException(ExceptionType.CantEstimateExecution, rpcException.Message);
                }
            }
            catch (ClientSideException e)
            {
                Console.WriteLine($"Exception: {e.Message}");
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e.Message}");
                throw new ClientSideException(ExceptionType.None, e.Message);
            }

            return new OperationEstimationV2Result()
            {
                GasAmount = estimatedGas.Value,
                GasPrice = gasPrice,
                EthAmount = amount,
                IsAllowed = isAllowed
            };
        }

        private async Task<bool> IsSmartContracAsync(string toAddress)
        {
            var code = await _web3.Eth.GetCode.SendRequestAsync(toAddress);

            return code != "0x";
        }
    }
}
