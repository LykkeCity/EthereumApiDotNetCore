using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Exceptions
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

        #region PrivateWallets

        NotEnoughFunds = 100,
        TransactionExists = 101,
        TransactionRequiresMoreGas = 102,

        #endregion
    }
}
