using Lykke.Job.EthereumCore.Core.Settings.JobSettings;
using Lykke.Job.EthereumCore.Core.Settings.SlackNotifications;

namespace Lykke.Job.EthereumCore.Core.Settings
{
    public class AppSettings
    {
        public EthereumCoreSettings EthereumCoreJob { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
    }
}