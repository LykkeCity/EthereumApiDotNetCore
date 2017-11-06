using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
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

    public class BytesDeserializer : IMessageDeserializer<string>
    {
        public string Deserialize(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }
    }


    public class BytesSerializer<T> : IRabbitMqSerializer<T>
    {
        public byte[] Serialize(T model)
        {
            string serialized = Newtonsoft.Json.JsonConvert.SerializeObject(model);

            return Encoding.UTF8.GetBytes(serialized);
        }
    }

    public class BytesDeserializer<T> : IMessageDeserializer<T>
    {
        public T Deserialize(byte[] data)
        {
            string encoded = Encoding.UTF8.GetString(data);
            T deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(encoded);

            return deserialized;
        }
    }

}
