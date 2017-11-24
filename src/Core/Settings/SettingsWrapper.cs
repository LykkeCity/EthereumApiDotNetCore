using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Settings
{
    public class SettingsWrapper
    {
        public BaseSettings EthereumCore { get; set; }
        public SlackNotificationSettings SlackNotifications { get; set; }
        public HotWalletSettings Ethereum { get; set; }
        public AssetsServiceSettings Assets { get; set; }
    }
}
