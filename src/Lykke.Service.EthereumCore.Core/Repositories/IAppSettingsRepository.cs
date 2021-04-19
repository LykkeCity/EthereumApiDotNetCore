using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface IAppSetting
    {
        string Key { get; }
        string Value { get; set; }
    }

    public class AppSetting : IAppSetting
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public interface IAppSettingsRepository
    {
        Task SetSettingAsync(string key, string value);

        Task<string> GetSettingAsync(string key);
        void DeleteTable();
    }
}
