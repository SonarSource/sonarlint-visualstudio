/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Interaction logic for SonarLintDaemonInstaller.xaml
    /// </summary>
    public partial class SonarLintDaemonInstaller : Window
    {
        private ISonarLintSettings settings;
        private ISonarLintDaemon daemon;

        private volatile bool canceled = false;

        private Action callback;

        public SonarLintDaemonInstaller()
        {
            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs args)
        {
            Daemon.Install(DownloadProgressChanged, DownloadFileCompleted);
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;

            ProgressBar.Value = (int) Math.Truncate(percentage);
        }

        private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                var ex = e.Error;
                var message = string.Format($"Failed to activate JavaScript support: {ex.Message}");
                MessageBox.Show(message, "Error", MessageBoxButton.OK);
                Debug.WriteLine(message + "\n" + ex.StackTrace);
                Close();
                return;
            }

            if (!canceled)
            {
                Daemon.Start();
                Settings.IsActivateMoreEnabled = true;
                if (callback != null)
                {
                    callback.DynamicInvoke();
                }
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

        private ISonarLintSettings Settings
        {
            get
            {
                if (this.settings == null)
                {
                    this.settings = ServiceProvider.GlobalProvider.GetMefService<ISonarLintSettings>();
                }

                return this.settings;
            }
        }

        private ISonarLintDaemon Daemon
        {
            get
            {
                if (this.daemon == null)
                {
                    this.daemon = ServiceProvider.GlobalProvider.GetMefService<ISonarLintDaemon>();
                }

                return this.daemon;
            }
        }
    }
}
