﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.EthereumCore.Core.Exceptions
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ExceptionType
    {
        None = 0,
        ContractPoolEmpty = 1,
        MissingRequiredParams = 2,
        WrongParams = 3,
        EntityAlreadyExists = 4,
        WrongSign = 5,
        OperationWithIdAlreadyExists = 6,
        TransferInProcessing = 7,
        WrongDestination = 8,
        CantEstimateExecution = 10,

        #region PrivateWallets

        NotEnoughFunds = 100,
        TransactionExists = 101,
        TransactionRequiresMoreGas = 102,

        #endregion
    }
}
