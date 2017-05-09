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
using System.Windows;
using Microsoft.VisualStudio.Shell;

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
            // TODO rewrite using Task.Run, see: https://blog.stephencleary.com/2013/05/taskrun-vs-backgroundworker-intro.html
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
