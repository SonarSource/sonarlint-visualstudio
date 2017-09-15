using System;
using System.Drawing;
using Forms = System.Windows.Forms;

namespace SonarLint.VisualStudio.Integration.Vsix.Notifications
{
    public class NotifyIconWrapper : INotifyIcon
    {
        private Forms.NotifyIcon notifyIcon = new Forms.NotifyIcon();

        public Forms.ToolTipIcon BalloonTipIcon
        {
            get
            {
                return notifyIcon.BalloonTipIcon;
            }
            set
            {
                notifyIcon.BalloonTipIcon = value;
            }
        }

        public string BalloonTipText
        {
            get
            {
                return notifyIcon.BalloonTipText;
            }
            set
            {
                notifyIcon.BalloonTipText = value;
            }
        }

        public string BalloonTipTitle
        {
            get
            {
                return notifyIcon.BalloonTipTitle;
            }
            set
            {
                notifyIcon.BalloonTipTitle = value;
            }
        }

        public Icon Icon
        {
            get
            {
                return notifyIcon.Icon;
            }
            set
            {
                notifyIcon.Icon = value;
            }
        }

        public string Text
        {
            get
            {
                return notifyIcon.Text;
            }
            set
            {
                notifyIcon.Text = value;
            }
        }

        public bool Visible
        {
            get
            {
                return notifyIcon.Visible;
            }
            set
            {
                notifyIcon.Visible = value;
            }
        }

        public event EventHandler BalloonTipClicked
        {
            add
            {
                notifyIcon.BalloonTipClicked += value;
            }
            remove
            {
                notifyIcon.BalloonTipClicked -= value;
            }
        }

        public event EventHandler Click
        {
            add
            {
                notifyIcon.Click += value;
            }
            remove
            {
                notifyIcon.Click -= value;
            }
        }

        public event EventHandler DoubleClick
        {
            add
            {
                notifyIcon.DoubleClick += value;
            }
            remove
            {
                notifyIcon.DoubleClick -= value;
            }
        }

        public void Dispose()
        {
            notifyIcon.Dispose();
            notifyIcon = null;
        }

        public void ShowBalloonTip(int timeout, string tipTitle, string tipText)
            => notifyIcon.ShowBalloonTip(timeout, tipTitle, tipText, Forms.ToolTipIcon.Info);
    }
}
