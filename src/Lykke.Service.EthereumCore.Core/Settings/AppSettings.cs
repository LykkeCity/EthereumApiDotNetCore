using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.EthereumCore.Core.Settings
{
    public class AppSettings
    {
        public BaseSettings EthereumCore { get; set; }
        public SlackNotificationSettings SlackNotifications { get; set; }
        public HotWalletSettings Ethereum { get; set; }
        public AssetsServiceSettings Assets { get; set; }
        [Lykke.SettingsReader.Attributes.Optional]
        public ChaosSettings ChaosKitty { get; set; }
    }
}
