using System;
using Lykke.Service.EthereumCore.Core;
using Newtonsoft.Json;
using AzureStorage.Queue;

namespace Lykke.Service.EthereumCore.Services
{
    //public interface IEmailNotifierService
    //{
    //    void Warning(string title, string message);
    //}

    //public class EmailNotifierService : IEmailNotifierService
    //{
    //    private readonly IQueueExt _queue;

    //    public EmailNotifierService(Func<string, IQueueExt> queueFactory)
    //    {
    //        _queue = queueFactory(Constants.EmailNotifierQueue);
    //    }

    //    public void Warning(string title, string message)
    //    {
    //        var obj = new
    //        {
    //            Data = new
    //            {
    //                BroadcastGroup = 100,
    //                MessageData = new
    //                {
    //                    Subject = title,
    //                    Text = message
    //                }
    //            }
    //        };

    //        var str = "PlainTextBroadcast:" + JsonConvert.SerializeObject(obj);

    //        _queue.PutRawMessageAsync(str);
    //    }
    //}
}
