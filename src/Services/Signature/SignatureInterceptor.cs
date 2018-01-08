using System;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;
using SigningServiceApiCaller;
using Core.Settings;
using Nethereum.RPC.TransactionManagers;

namespace LkeServices.Signature
{
    public class SignatureInterceptor : RequestInterceptor
    {
        private readonly ITransactionManager _transactionManager;

        public SignatureInterceptor(ITransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        #region RC-6

        public override async Task<object> InterceptSendRequestAsync<T>(
            Func<Nethereum.JsonRpc.Client.RpcRequest, string, Task<T>> interceptedSendRequestAsync, Nethereum.JsonRpc.Client.RpcRequest request,
            string route = null)
        {
            if (request.Method == "eth_sendTransaction")
            {
                TransactionInput transaction = (TransactionInput)request.RawParameters[0];
                var response = await SignAndSendTransaction(transaction, route);
                return response;
            }

            return await interceptedSendRequestAsync(request, route).ConfigureAwait(false);
        }

        public override async Task InterceptSendRequestAsync(
            Func<Nethereum.JsonRpc.Client.RpcRequest, string, Task> interceptedSendRequestAsync, Nethereum.JsonRpc.Client.RpcRequest request,
            string route = null)
        {
            if (request.Method == "eth_sendTransaction")
            {
                TransactionInput transaction = (TransactionInput)request.RawParameters[0];
                var response = await SignAndSendTransaction(transaction, route);
                return;
            }

            await interceptedSendRequestAsync(request, route).ConfigureAwait(false);
        }

        public override async Task<object> InterceptSendRequestAsync<T>(
            Func<string, string, object[], Task<T>> interceptedSendRequestAsync, string method,
            string route = null, params object[] paramList)
        {
            if (method == "eth_sendTransaction")
            {
                TransactionInput transaction = (TransactionInput)paramList[0];
                var response = await SignAndSendTransaction(transaction, route);
                return response;
            }

            return await interceptedSendRequestAsync(method, route, paramList).ConfigureAwait(false);
        }

        public override Task InterceptSendRequestAsync(
            Func<string, string, object[], Task> interceptedSendRequestAsync, string method,
            string route = null, params object[] paramList)
        {
            if (method == "eth_sendTransaction")
            {
                TransactionInput transaction = (TransactionInput)paramList[0];
                var response = SignAndSendTransaction(transaction, route);
                return response;
            }

            return interceptedSendRequestAsync(method, route, paramList);
        }

        #endregion

        private async Task<string> SignAndSendTransaction(TransactionInput transaction, string route)
        {
            return await _transactionManager.SendTransactionAsync(transaction).ConfigureAwait(false);
        }
    }
}
