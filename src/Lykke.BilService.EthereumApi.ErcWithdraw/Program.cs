using Lykke.Common.Log;
using Lykke.Quintessence.Core.Blockchain;
using Lykke.Quintessence.Core.Utils;
using Lykke.Service.BlockchainApi.Client.Models;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.EthereumCore.Client.Models;
using Microsoft.Extensions.Logging.Console;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.Util;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;
using System;
using System.Numerics;
using System.Threading.Tasks;
using SignTransactionRequest = Lykke.Service.BlockchainSignFacade.Contract.Models.SignTransactionRequest;

namespace Lykke.BilService.EthereumApi.ErcWithdraw
{
    class Program
    {
        public const string Erc20ABI =
            "[{\"constant\":false,\"inputs\":[{\"name\":\"_spender\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"approve\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"totalSupply\",\"outputs\":[{\"name\":\"supply\",\"type\":\"uint256\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_from\",\"type\":\"address\"},{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"transferFrom\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"balance\",\"type\":\"uint256\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"transfer\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"},{\"name\":\"_spender\",\"type\":\"address\"}],\"name\":\"allowance\",\"outputs\":[{\"name\":\"remaining\",\"type\":\"uint256\"}],\"payable\":false,\"type\":\"function\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"name\":\"from\",\"type\":\"address\"},{\"indexed\":true,\"name\":\"to\",\"type\":\"address\"},{\"indexed\":false,\"name\":\"value\",\"type\":\"uint256\"}],\"name\":\"Transfer\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"name\":\"from\",\"type\":\"address\"},{\"indexed\":true,\"name\":\"spender\",\"type\":\"address\"},{\"indexed\":false,\"name\":\"value\",\"type\":\"uint256\"}],\"name\":\"Approval\",\"type\":\"event\"}]";

        public static string componentName = "Lykke.BilService.EthereumApi.ErcWithdraw";

        static async Task Main(string[] args)
        {

            if (args.Length != 11)
                Console.WriteLine("Wrong number of arguments, it should be equal to 11");
            else
            {
                await TransferErc20Async(
                    args[0],
                    args[1],
                    args[2],
                    BigInteger.Parse(args[3]),
                    args[4],
                    args[5],
                    args[6],
                    args[7],
                    args[8],
                    args[9],
                    int.Parse(args[10]));
            }
            Console.ReadKey();
        }

        private static async Task TransferErc20Async(string fromAddress,
            string toAddress,
            string hotWalletAddress,
            BigInteger amountToTransfer,
            string erc20ContractAddress,
            string ethereumCoreApi,
            string ethBilApi,
            string signFacadeApi,
            string signFacadeApiKey,
            string parityUrl,
            int gasLimit)
        {
            var web3 = new Web3(parityUrl);
            var logFactory = Lykke.Logs.LogFactory.Create();
            var log = logFactory.CreateLog(componentName);
            logFactory.AddProvider(new ConsoleLoggerProvider((x, level) => true, true));
            var ethCoreClient =
                new Lykke.Service.EthereumCore.Client.EthereumCoreAPI(new Uri(ethereumCoreApi));
            var ethBilClient =
                new Lykke.Service.BlockchainApi.Client.BlockchainApiClient(logFactory, ethBilApi);
            var facadeClient =
                new Lykke.Service.BlockchainSignFacade.Client.BlockchainSignFacadeClient(signFacadeApi,
                    signFacadeApiKey,
                    logFactory.CreateLog(componentName));

            await ethBilClient.StartBalanceObservationAsync(fromAddress);
            var isObservationStopped = await ethBilClient.StopBalanceObservationAsync(fromAddress);

            if (!isObservationStopped)
            {
                log.Warning("Can't stop observation. Stopping app.");

                return;
            }

            var gasResponse = await ethCoreClient.ApiRpcGetNetworkGasPriceGetWithHttpMessagesAsync();
            var gasPrice = gasResponse.Body as BalanceModel;

            var erc20PrivateWalletEstimation = new PrivateWalletErc20EstimateTransaction(erc20ContractAddress,
                amountToTransfer.ToString(),
                "0",
                fromAddress,
                toAddress,
                gasPrice.Amount //5Gwei
            );

            BigInteger ethAmountToTransferErc20 = gasLimit * BigInteger.Parse(gasPrice.Amount);
            var res = ((decimal)ethAmountToTransferErc20 / (decimal)BigInteger.Pow(10, 18));

            var operationId = Guid.NewGuid();
            var blockchainAsset = new BlockchainAsset(new AssetContract()
            {
                Accuracy = 18,
                Address = null,
                AssetId = "ETH",
                Name = "Ethereum"
            });
            var result1 = await ethBilClient.BuildSingleTransactionAsync(operationId,
                hotWalletAddress,
                null,
                fromAddress,
                blockchainAsset,
                res, 
                false);

            var signedTransactionFromHw = await facadeClient.SignTransactionAsync("Ethereum", new SignTransactionRequest()
            {
                PublicAddresses = new[] { hotWalletAddress },
                TransactionContext = result1.TransactionContext
            });

            var brResult = await ethBilClient.BroadcastTransactionAsync(operationId, signedTransactionFromHw.SignedTransaction);
            BroadcastedSingleTransaction broadcasted;

            do
            {
                broadcasted = await ethBilClient.TryGetBroadcastedSingleTransactionAsync(operationId, blockchainAsset);
                await Task.Delay(TimeSpan.FromSeconds(7));
            } while (broadcasted.State != BroadcastedTransactionState.Completed);

            var esResult = await ethCoreClient.ApiEstimationEstimateTransactionErc20PostWithHttpMessagesAsync(erc20PrivateWalletEstimation);
            var estimation = esResult.Body as EstimatedGasModelV2;

            var erc20PrivateWalletTransaction = new PrivateWalletErc20Transaction(erc20ContractAddress,
                amountToTransfer.ToString(),
                "0",
                fromAddress,
                toAddress,
                estimation.EstimatedGas,
                estimation.GasPrice //5Gwei
            );
            string data = GetTransferFunctionCallEncoded(web3, erc20ContractAddress, toAddress, amountToTransfer);
            AddressUtil util = new AddressUtil();
            var result = await ethCoreClient.ApiErc20WalletGetTransactionPostWithHttpMessagesAsync(erc20PrivateWalletTransaction);
            var trResponse = result.Body as EthTransactionRaw;
            var nonce = await GetNonceAsync(web3, fromAddress, true);

            DefaultTransactionParams @params = new DefaultTransactionParams(0, 
                data, 
                fromAddress, 
                BigInteger.Parse(estimation.EstimatedGas),
                BigInteger.Parse(estimation.GasPrice),
                nonce.Value,
                erc20ContractAddress);
            var inHexEncodingTr = Newtonsoft.Json.JsonConvert.SerializeObject(@params).ToHex();

            var signedTransaction = await facadeClient.SignTransactionAsync("Ethereum", new SignTransactionRequest()
            {
                PublicAddresses = new[] { util.ConvertToChecksumAddress(fromAddress) },
                TransactionContext = inHexEncodingTr
            });


            await ethCoreClient.ApiPrivateWalletSubmitTransactionPostWithHttpMessagesAsync(
                new PrivateWalletEthSignedTransaction(fromAddress, signedTransaction.SignedTransaction));

            await ethBilClient.StartBalanceObservationAsync(fromAddress);
        }

        private static Contract GetContract(Web3 web3, string erc20ContactAddress)
        {
            Contract contract = web3.Eth.GetContract(Erc20ABI, erc20ContactAddress);

            return contract;
        }

        public static string GetTransferFunctionCallEncoded(Web3 web3, string tokenAddress, string receiverAddress, BigInteger amount)
        {
            Contract contract = GetContract(web3, tokenAddress);
            Function transferFunction = contract.GetFunction("transfer");
            string functionDataEncoded = transferFunction.GetData(receiverAddress, amount);
            return functionDataEncoded;
        }

        private static async Task<HexBigInteger> GetNonceAsync(Web3 web3, string fromAddress, bool checkTxPool)
        {
            var txPool = await web3.Client.SendRequestAsync<JValue>(new RpcRequest($"{Guid.NewGuid()}", "parity_nextNonce", fromAddress));

            var bigInt = new HexBigInteger(txPool.Value.ToString());
            return bigInt;
        }
    }
}
