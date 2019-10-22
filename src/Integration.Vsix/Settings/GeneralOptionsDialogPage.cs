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
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class GeneralOptionsDialogPage : UIElementDialogPage
    {
        public const string PageName = "General";

        private GeneralOptionsDialogControl dialogControl;
        private ISonarLintSettings settings;

        protected override UIElement Child
        {
            get
            {
                if (dialogControl == null)
                {
                    Debug.Assert(this.Site != null, "Expecting the page to be sited");
                    var daemon = this.Site.GetMefService<ISonarLintDaemon>();
                    var installer = this.Site.GetMefService<IDaemonInstaller>();
                    var userSettingsProvider = this.Site.GetMefService<IUserSettingsProvider>();
                    var logger = this.Site.GetMefService<ILogger>();

                    var openSettingsFileCmd = new OpenSettingsFileWpfCommand(this.Site, userSettingsProvider, logger);
                    dialogControl = new GeneralOptionsDialogControl(Settings, daemon, installer, openSettingsFileCmd, logger);
                }
                return dialogControl;
            }
        }

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);

            dialogControl.DaemonVerbosity.ItemsSource = Enum.GetValues(typeof(DaemonLogLevel)).Cast<DaemonLogLevel>();
            dialogControl.DaemonVerbosity.SelectedItem = Settings.DaemonLogLevel;
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            if (e.ApplyBehavior == ApplyKind.Apply)
            {
                Settings.DaemonLogLevel = (DaemonLogLevel)dialogControl.DaemonVerbosity.SelectedItem;
            }

            base.OnApply(e);
        }

        private ISonarLintSettings Settings
        {
            get
            {
                if (this.settings == null)
                {
                    Debug.Assert(this.Site != null, "Expecting the page to be sited");
                    this.settings = this.Site.GetMefService<ISonarLintSettings>();
                }

                return this.settings;
            }
        }
    }
}
