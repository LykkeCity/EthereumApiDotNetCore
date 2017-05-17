using Lykke.RabbitMqBroker.Publisher;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQ
{
    public class BytesSerializer : IRabbitMqSerializer<string>
    {
        public byte[] Serialize(string model)
        {
            return Encoding.UTF8.GetBytes(model);
        }
    }
}
