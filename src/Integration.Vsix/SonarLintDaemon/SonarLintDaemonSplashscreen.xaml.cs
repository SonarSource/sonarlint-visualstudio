using System.ComponentModel;
using System.Windows;
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Interaction logic for SonarLintDaemonSplashscreen.xaml
    /// </summary>
    public partial class SonarLintDaemonSplashscreen : Window
    {
        public SonarLintDaemonSplashscreen()
        {
            InitializeComponent();
        }

        private void ClickYes(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            var settings = ServiceProvider.GlobalProvider.GetMefService<ISonarLintSettings>();
            settings.SkipActivateMoreDialog = SkipActivateMoreDialogCheckBox.IsChecked.Value;
        }
    }
}
