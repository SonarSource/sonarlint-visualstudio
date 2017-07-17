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

            UpdateActiveMoreControls();
        }

        private void UpdateActiveMoreControls()
        {
            if (Settings.IsActivateMoreEnabled)
            {
                ActivateButton.Visibility = Visibility.Collapsed;
                ActivateText.Visibility = Visibility.Collapsed;
                DeactivateButton.Visibility = Visibility.Visible;
                DeactivateText.Visibility = Visibility.Visible;
                VerbosityPanel.Visibility = Visibility.Visible;
            }
            else
            {
                ActivateButton.Visibility = Visibility.Visible;
                ActivateText.Visibility = Visibility.Visible;
                DeactivateButton.Visibility = Visibility.Collapsed;
                DeactivateText.Visibility = Visibility.Collapsed;
                VerbosityPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void OnActivateMoreClicked(object sender, RoutedEventArgs e)
        {
            if (!Daemon.IsInstalled)
            {
                new SonarLintDaemonInstaller().Show(UpdateActiveMoreControls);
                return;
            }

            if (!Daemon.IsRunning)
            {
                Daemon.Start();
            }
            Settings.IsActivateMoreEnabled = true;

            UpdateActiveMoreControls();
        }

        private void OnDeactivateClicked(object sender, RoutedEventArgs e)
        {
            if (Daemon.IsRunning)
            {
                Daemon.Stop();
            }
            Settings.IsActivateMoreEnabled = false;

            UpdateActiveMoreControls();
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
