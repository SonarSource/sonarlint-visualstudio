using System;
using System.Drawing;

namespace SonarLint.VisualStudio.Integration.Vsix.Notifications
{
    public interface INotifyIcon : IDisposable
    {
        Icon Icon { get; set; }
        string Text { get; set; }
        bool Visible { get; set; }

        event EventHandler BalloonTipClicked;
        event EventHandler Click;
        event EventHandler DoubleClick;

        void ShowBalloonTip(int timeout, string tipTitle, string tipText);
    }
}
