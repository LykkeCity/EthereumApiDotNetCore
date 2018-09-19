using Autofac;
using Common.Log;
using Lykke.Service.EthereumCore.Core.Settings;
using Lykke.SettingsReader;

namespace DepositContractResolver.Helpers
{
    public interface IConfigurationHelper
    {
        IReloadingManager<AppSettings> GetCurrentSettingsFromUrl(string settingsUrl);

        (IContainer resolver, ILog logToConsole) GetResolver(IReloadingManager<AppSettings> appSettings);
    }
}
