﻿namespace Lykke.Service.EthereumCore.Core.Settings
{
    public class AppSettings
    {
        public BaseSettings EthereumCore { get; set; }
        public SlackNotificationSettings SlackNotifications { get; set; }
        public HotWalletSettings Ethereum { get; set; }
        public AssetsServiceSettings Assets { get; set; }
        [Lykke.SettingsReader.Attributes.Optional]
        public ChaosSettings ChaosKitty { get; set; }
        public LykkePay LykkePay { get; set; }
        public Airlines Airlines { get; set; }
        public ApiKeys ApiKeys { get; set; }
        public BlockPassClientSettings BlockPassClient { get; set; }
    }
}
