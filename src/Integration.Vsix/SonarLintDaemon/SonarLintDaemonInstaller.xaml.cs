using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.Windows;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Interaction logic for SonarLintDaemonInstaller.xaml
    /// </summary>
    public partial class SonarLintDaemonInstaller : Window
    {
        public SonarLintDaemonInstaller()
        {
            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs args)
        {
            try
            {
                var daemon = ServiceProvider.GlobalProvider.GetMefService<ISonarLintDaemon>();
                daemon.Install();
                daemon.Start();
                ServiceProvider.GlobalProvider.GetMefService<ISonarLintSettings>().SkipActivateMoreDialog = true;
            }
            catch (Exception e)
            {
                var message = string.Format("Failed to activate JavaScript support: {0}", e.Message);
                MessageBox.Show(message, "Error", MessageBoxButton.OK);
                Debug.WriteLine(message + "\n" + e.StackTrace);
            }
        }
    }
}
