using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Utils;

namespace AzureRepositories.Azure.Tables
{
    public static class TableStorageClassSerializer
    {

        public static string SerializeArrayForTableStorage<T>(this IEnumerable<T> data)
        {
            if (data == null)
                return null;

            if (!data.Any())
                return null;

            return data.ToJson();
        }

        public static T[] DeserializeArrayForTableStorage<T>(this string data)
        {
            if (data == null)
                return new T[0];
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T[]>(data);
            }
            catch (Exception)
            {
                
                return new T[0];
            }

        }
    }

    public class AzureStorageField<T>
    {

        private T _value;
        private string _stringValue;

        public T GetVaule()
        {

            return _value;
        }

        protected string Serialize(T value)
        {
            if (typeof (T).GetTypeInfo().IsEnum)
                return value.ToString();

            return value.ToJson();
        }

        protected T DeSerialize(string value)
        {
            if (typeof(T).GetTypeInfo().IsEnum)
                return value.ParseEnum<T>();

            return value.DeserializeJson<T>();
        }

        public void SetValue(T value)
        {
            _value = value;
            _stringValue = Serialize(value);
        }

        public void SetString(string value)
        {
            _stringValue = value;
            _value = DeSerialize(value);
        }

        public string GetString()
        {
            return _stringValue;
        }
    }

    public class AzureStorageIEnumerableField<T>
    {

        private T[] _value;
        private string _stringValue;

        private static readonly T[] NullArray = new T[0];


        public T[] GetVaule()
        {

            return _value;
        }

        public void SetValue(IEnumerable<T> data)
        {
            if (data == null)
            {
                _value = NullArray;
                _stringValue = null;
            }
            else
            {
                _value = data as T[] ?? data.ToArray();
                _stringValue = _value.SerializeArrayForTableStorage();
            }
        }

        public void SetString(string data)
        {
            _stringValue = data;
            _value = data.DeserializeArrayForTableStorage<T>();
        }

        public string GetString()
        {
            return _stringValue;
        }
    }





}
