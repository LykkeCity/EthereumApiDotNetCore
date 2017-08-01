﻿using System;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Core;
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

        public RpcResponse BuildResponse(object results, string route = null)
        {
            return new RpcResponse(route, JToken.FromObject(results));
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
                return response.Result.ToObject<T>();
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
                return ;
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
                return response.Result.ToObject<T>();
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






        //public override async Task<RpcResponse> InterceptSendRequestAsync(Func<Nethereum.JsonRpc.Client.RpcRequest, string, Task<RpcResponse>> interceptedSendRequestAsync, Nethereum.JsonRpc.Client.RpcRequest request, string route = null)
        //{
        //    if (request.Method == "eth_sendTransaction")
        //    {
        //        TransactionInput transaction = (TransactionInput)request.RawParameters[0];
        //        return await SignAndSendTransaction(transaction, route);
        //    }
        //    return await interceptedSendRequestAsync(request, route).ConfigureAwait(false);
        //}

        //public override async Task<RpcResponse> InterceptSendRequestAsync<RpcResponse>(Func<string, string, object[], Task<RpcResponse>> interceptedSendRequestAsync, string method, string route = null, params object[] paramList)
        //{
        //    if (method == "eth_sendTransaction")
        //    {
        //        TransactionInput transaction = (TransactionInput)paramList[0];
        //        return await SignAndSendTransaction(transaction, route);
        //    }
        //    return await interceptedSendRequestAsync(method, route, paramList).ConfigureAwait(false);
        //}

        private async Task<RpcResponse> SignAndSendTransaction(TransactionInput transaction, string route)
        {
            return BuildResponse(await _transactionManager.SendTransactionAsync(transaction).ConfigureAwait(false), route);
        }

        /*
          [AsyncStateMachine(typeof(RequestInterceptor.<InterceptSendRequestAsync>d__0<>))]
        [DebuggerStepThrough]
        public virtual Task<object> InterceptSendRequestAsync<T>(Func<RpcRequest, string, Task<T>> interceptedSendRequestAsync, RpcRequest request, string route = null);
        [AsyncStateMachine(typeof(<InterceptSendRequestAsync>d__1))]
        [DebuggerStepThrough]
        public virtual Task InterceptSendRequestAsync(Func<RpcRequest, string, Task> interceptedSendRequestAsync, RpcRequest request, string route = null);
        [AsyncStateMachine(typeof(RequestInterceptor.<InterceptSendRequestAsync>d__2<>))]
        [DebuggerStepThrough]
        public virtual Task<object> InterceptSendRequestAsync<T>(Func<string, string, object[], Task<T>> interceptedSendRequestAsync, string method, string route = null, params object[] paramList);
        public virtual Task InterceptSendRequestAsync(Func<string, string, object[], Task> interceptedSendRequestAsync, string method, string route = null, params object[] paramList);
         */
    }
}
