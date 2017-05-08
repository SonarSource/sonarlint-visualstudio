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

using Microsoft.VisualStudio.Shell;
using System.Windows;
using System.Windows.Controls;
using System;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Interaction logic for GeneralOptionsDialogControl.xaml
    /// </summary>
    public partial class GeneralOptionsDialogControl : UserControl
    {
        private ISonarLintSettings settings;
        private ISonarLintDaemon daemon;

        public GeneralOptionsDialogControl()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            UpdateActivateMoreButtonText();
        }

        private void UpdateActivateMoreButtonText()
        {
            ActivateMoreButton.Content = Settings.IsActivateMoreEnabled
                ? "Deactivate JavaScript support"
                : "Install and activate JavaScript support";
        }

        private void OnActivateMoreClicked(object sender, RoutedEventArgs e)
        {
            if (Settings.IsActivateMoreEnabled)
            {
                if (Daemon.IsRunning)
                {
                    Daemon.Stop();
                }
            }
            else
            {
                if (!Daemon.IsInstalled)
                {
                    new SonarLintDaemonInstaller().Show();
                }
                else if (!Daemon.IsRunning)
                {
                    Daemon.Start();
                }
            }

            Settings.IsActivateMoreEnabled = !Settings.IsActivateMoreEnabled;
            UpdateActivateMoreButtonText();
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
