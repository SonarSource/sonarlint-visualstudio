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
using Microsoft.VisualStudio;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Interaction logic for SonarLintDaemonInstaller.xaml
    /// </summary>
    public partial class SonarLintDaemonInstaller : Window
    {
        private readonly ISonarLintSettings settings;
        private readonly ISonarLintDaemon daemon;
        private readonly ILogger logger;

        private volatile bool canceled = false;

        private Action callback;

        public SonarLintDaemonInstaller(ISonarLintSettings settings, ISonarLintDaemon daemon, ILogger logger)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (daemon == null)
            {
                throw new ArgumentNullException(nameof(daemon));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.settings = settings;
            this.daemon = daemon;
            this.logger = logger;

            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs args)
        {
            try
            {
                ProgressBar.Visibility = Visibility.Visible;
                CompletedMessage.Visibility = Visibility.Collapsed;

                this.logger.WriteLine(Strings.Daemon_DownloadingDaemon);
                daemon.DownloadProgressChanged += DownloadProgressChanged;
                daemon.DownloadCompleted += DownloadCompleted;
                this.logger.WriteLine(Strings.Daemon_DaemonDownloaded);

                daemon.Install();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.ERROR_InstallingDaemon, ex);
            }
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                double bytesIn = double.Parse(e.BytesReceived.ToString());
                double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                double percentage = bytesIn / totalBytes * 100;

                ProgressBar.Value = (int)Math.Truncate(percentage);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.ERROR_InstallingDaemon, ex);
            }
        }

        private void DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            try
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
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.ERROR_InstallingDaemon, ex);
            }
        }

        internal void Show(Action callback)
        {
            this.logger.WriteLine(Strings.Daemon_InstallingDaemon);
            this.callback = callback;
            Show();
            this.logger.WriteLine(Strings.Daemon_DaemonInstalled);
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
