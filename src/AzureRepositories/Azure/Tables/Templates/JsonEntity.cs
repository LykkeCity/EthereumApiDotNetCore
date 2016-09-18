using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace AzureRepositories.Azure.Tables.Templates
{
    /// <summary>
    /// Используем для сохранения сложный объектов (с листами, с объектами)
    /// </summary>
    /// <typeparam name="T">Тип, который сохраняем</typeparam>
    public class JsonTableEntity<T> : TableEntity 
    {
        public T Instance;

        public string Data
        {
            get
            {
                return JsonConvert.SerializeObject(Instance);
            }
            set
            {
                Instance = JsonConvert.DeserializeObject<T>(value);
            }
        }

    }
}
