using System;
using System.Collections.Generic;
using System.Text;
using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC;
using Nethereum.RPC.TransactionManagers;

namespace Lykke.Service.EthereumCore.Core.Services
{
    public interface IWeb3
    {
        ShhApiService Shh { get; }
        EthApiContractService Eth { get; }
        IClient Client { get; }
        ITransactionManager TransactionManager { get; set; }
        PersonalApiService Personal { get; }
        NetApiService Net { get; }
    }
}
