/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Interaction logic for SonarLintDaemonInstaller.xaml
    /// </summary>
    public partial class SonarLintDaemonInstaller : Window
    {
        private readonly ISonarLintSettings settings;
        private readonly ISonarLintDaemon daemon;

        private volatile bool canceled = false;

        private Action callback;

        public SonarLintDaemonInstaller(ISonarLintSettings settings, ISonarLintDaemon daemon)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (daemon == null)
            {
                throw new ArgumentNullException(nameof(daemon));
            }

            this.settings = settings;
            this.daemon = daemon;

            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs args)
        {
            ProgressBar.Visibility = Visibility.Visible;
            CompletedMessage.Visibility = Visibility.Collapsed;

            daemon.DownloadProgressChanged += DownloadProgressChanged;
            daemon.DownloadCompleted += DownloadCompleted;
            daemon.Install();
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;

            ProgressBar.Value = (int) Math.Truncate(percentage);
        }

        private void DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            daemon.DownloadProgressChanged -= DownloadProgressChanged;
            daemon.DownloadCompleted -= DownloadCompleted;

            if (e.Error != null)
            {
                var ex = e.Error;
                var message = string.Format($"Failed to activate support of additional languages: {ex.Message}");
                MessageBox.Show(message, "Error", MessageBoxButton.OK);
                Debug.WriteLine(message + "\n" + ex.StackTrace);
                Close();
                return;
            }

            if (!canceled)
            {
                ProgressBar.Visibility = Visibility.Collapsed;
                CompletedMessage.Visibility = Visibility.Visible;

                daemon.Start();
                settings.IsActivateMoreEnabled = true;
                callback?.DynamicInvoke();
            }

            OkButton.IsEnabled = true;
            OkButton.Focus();
        }

        internal void Show(Action callback)
        {
            this.callback = callback;
            Show();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            canceled = true;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
