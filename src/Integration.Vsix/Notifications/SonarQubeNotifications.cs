using System;
using System.Drawing;
using System.Timers;
using System.Windows;
using SonarLint.VisualStudio.Integration.Service;
using SystemInterface.Timers;
using Forms = System.Windows.Forms;

namespace SonarLint.VisualStudio.Integration.Vsix.Notifications
{
    public class SonarQubeNotifications : IDisposable
    {
        private readonly INotifyIconFactory notifyIconFactory;
        private readonly ITimerFactory timerFactory;

        private QualityGateStatus currentStatus;

        private INotifyIcon notifyIcon;
        private ITimer timer;

        public event EventHandler ShowDetails;

        public SonarQubeNotifications(INotifyIconFactory notifyIconFactory, ITimerFactory timerFactory)
        {
            this.notifyIconFactory = notifyIconFactory;
            this.timerFactory = timerFactory;
        }

        public void Start()
        {
            if (notifyIcon == null)
            {
                notifyIcon = notifyIconFactory.Create();
                notifyIcon.Click += (s, e) => ShowNofitication();
                notifyIcon.DoubleClick += (s, e) => OnShowDetails(EventArgs.Empty);
                notifyIcon.BalloonTipClicked += (s, e) => OnShowDetails(EventArgs.Empty);
            }

            notifyIcon.Text = "Initializing...";
            notifyIcon.Icon = GetIcon(QualityGateStatus.Passed);
            notifyIcon.Visible = true;

            if (timer == null)
            {
                timer = timerFactory.Create();
                timer.Elapsed += OnTimerElapsed;
            }

            timer.Interval = 10000d;
            timer.Start();
        }

        public void Stop()
        {
            if (timer != null)
            {
                timer.Elapsed -= OnTimerElapsed;
                timer?.Stop();
            }

            if (notifyIcon != null)
            {
                notifyIcon.Dispose();
                notifyIcon = null;
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var newStatus = GetStatus();

            if (newStatus != currentStatus)
            {
                notifyIcon.Icon = GetIcon(newStatus);

                if (currentStatus == QualityGateStatus.Failed)
                {
                    ShowNofitication();
                }

                currentStatus = newStatus;
            }
        }

        private Icon GetIcon(QualityGateStatus status)
        {
            var iconPath = status == QualityGateStatus.Passed
                ? "pack://application:,,,/SonarLint;component/Resources/sonarqube_green.ico"
                : "pack://application:,,,/SonarLint;component/Resources/sonarqube_red.ico";

            return new Icon(Application.GetResourceStream(new Uri(iconPath)).Stream);
        }

        private QualityGateStatus GetStatus()
        {
            var random = new Random();
            var i = random.Next(100);

            return i % 2 == 0
                ? QualityGateStatus.Passed
                : QualityGateStatus.Failed;
        }

        private string GetText()
        {
            return $"QualityGate '{currentStatus}' for 'SonarC#'";
        }

        private string GetStatusText()
        {
            return currentStatus.ToString();
        }

        private void ShowNofitication()
        {
            notifyIcon?.ShowBalloonTip(10000, GetText(), GetStatusText());
        }

        private void OnShowDetails(EventArgs e)
        {
            ShowDetails?.Invoke(this, e);
        }

        public void Dispose()
        {
            Stop();
            timer = null;
        }
    }
}
