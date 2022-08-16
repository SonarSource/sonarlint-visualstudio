using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Core.SonarLintNotifications
{
    public interface INotificationSetting
    {
        string Key { get; }
        bool DoNotShow { get; }
    }

    public class NotificationSetting : INotificationSetting
    {
        public string Key { get;  }
        public bool DoNotShow { get; }
    }
}
