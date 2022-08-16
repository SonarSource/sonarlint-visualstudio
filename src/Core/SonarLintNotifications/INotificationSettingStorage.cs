using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Core.SonarLintNotifications
{
    public enum StorageLocation
    {
        User,
        VSInstance,
        Solution
    }

    public interface INotificationSettingStorage
    {
        void SaveNotificationSetting(INotificationSetting setting, StorageLocation storageLocation);
        void GetNotificationSetting(string notificationSettingKey, StorageLocation storageLocation);
    }
}
