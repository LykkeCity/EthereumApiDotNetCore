using System;
using System.IO;
using System.Net.Http;

namespace Lykke.Service.EthereumCore.Core.Settings
{
    public static class GeneralSettingsReader
    {
        public static T ReadGeneralSettings<T>(string url)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url);
            var settingsData = httpClient.GetStringAsync("").Result;

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(settingsData);
        }

        public static T ReadGeneralSettingsLocal<T>(string path)
        {
            var content = File.ReadAllText(path);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);
        }

        [Obsolete("Use ReadGeneralSettings instead of this method")]
        public static T ReadSettingsFromData<T>(string jsonData)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonData);
        }
    }
}
