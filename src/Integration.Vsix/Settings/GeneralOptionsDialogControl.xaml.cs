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
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Interaction logic for GeneralOptionsDialogControl.xaml
    /// </summary>
    public partial class GeneralOptionsDialogControl : UserControl
    {
        private static readonly string ACTIVATE_LABEL = "Install and activate JavaScript support";
        private static readonly string DEACTIVATE_LABEL = "Deactivate JavaScript support";

        private ISonarLintSettings settings;
        private ISonarLintDaemon daemon;

        public GeneralOptionsDialogControl()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            if (!Daemon.IsInstalled)
            {
                Settings.IsActivateMoreEnabled = false;
            }

            UpdateActivateMoreButtonText();
        }

        private void UpdateActivateMoreButtonText()
        {
            ActivateMoreButton.Content = Settings.IsActivateMoreEnabled
                ? DEACTIVATE_LABEL
                : ACTIVATE_LABEL;
        }

        private void OnActivateMoreClicked(object sender, RoutedEventArgs e)
        {
            if (ActivateMoreButton.Content.Equals(DEACTIVATE_LABEL))
            {
                if (Daemon.IsRunning)
                {
                    Daemon.Stop();
                }
                Settings.IsActivateMoreEnabled = false;
            }
            else
            {
                if (!Daemon.IsInstalled)
                {
                    new SonarLintDaemonInstaller().Show(UpdateActivateMoreButtonText);
                }
                else if (!Daemon.IsRunning)
                {
                    Daemon.Start();
                    Settings.IsActivateMoreEnabled = true;
                }
                else
                {
                    Settings.IsActivateMoreEnabled = true;
                }
            }

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
