using AzureStorage;
using Common.Log;
using System;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.AzureRepositories.Log
{
    //public class LogToTable : ILog
    //{
    //    private readonly INoSQLTableStorage<LogEntity> _errorTableStorage;
    //    private readonly INoSQLTableStorage<LogEntity> _warningTableStorage;
    //    private readonly INoSQLTableStorage<LogEntity> _infoTableStorage;

    //    public LogToTable(INoSQLTableStorage<LogEntity> errorTableStorage,
    //                        INoSQLTableStorage<LogEntity> warningTableStorage,
    //                        INoSQLTableStorage<LogEntity> infoTableStorage)
    //    {
    //        _errorTableStorage = errorTableStorage;
    //        _warningTableStorage = warningTableStorage;
    //        _infoTableStorage = infoTableStorage;
    //    }


    //    private async Task Insert(string level, string component, string process, string context, string type, string stack,
    //        string msg, DateTime? dateTime)
    //    {
    //        var dt = dateTime ?? DateTime.UtcNow;
    //        var newEntity = LogEntity.Create(level, component, process, context, type, stack, msg, dt);

    //        if (level == "error" || level == "fatalerror")
    //            await _errorTableStorage.InsertAndGenerateRowKeyAsTimeAsync(newEntity, dt);
    //        if (level == "warning")
    //            await _warningTableStorage.InsertAndGenerateRowKeyAsTimeAsync(newEntity, dt);
    //        if (level == "info")
    //            await _infoTableStorage.InsertAndGenerateRowKeyAsTimeAsync(newEntity, dt);
    //    }

    //    public Task WriteInfoAsync(string component, string process, string context, string info, DateTime? dateTime = null)
    //    {
    //        return Insert("info", component, process, context, null, null, info, dateTime);
    //    }

    //    public Task WriteWarningAsync(string component, string process, string context, string info, DateTime? dateTime = null)
    //    {
    //        return Insert("warning", component, process, context, null, null, info, dateTime);
    //    }

    //    public Task WriteErrorAsync(string component, string process, string context, Exception type, DateTime? dateTime = null)
    //    {
    //        return Insert("error", component, process, context, type.GetType().ToString(), type.StackTrace, type.Message, dateTime);
    //    }

    //    public Task WriteFatalErrorAsync(string component, string process, string context, Exception type, DateTime? dateTime = null)
    //    {
    //        return Insert("fatalerror", component, process, context, type.GetType().ToString(), type.StackTrace, type.Message, dateTime);
    //    }
    //}
}
