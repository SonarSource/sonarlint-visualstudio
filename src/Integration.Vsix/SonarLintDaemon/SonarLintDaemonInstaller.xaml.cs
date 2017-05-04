using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
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
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.RunWorkerAsync();
        }

        void worker_DoWork(object sender, DoWorkEventArgs eventArgs)
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

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            pbStatus.Value = 100;
            pbStatus.IsIndeterminate = false;
            okButton.IsEnabled = true;
            okButton.Focus();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
