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
using System.ComponentModel;
using System.Windows;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class GeneralOptionsDialogPage : UIElementDialogPage
    {
        public const string PageName = "General";

        private GeneralOptionsDialogControl dialogControl;
        private ISonarLintSettings settings;

        protected override UIElement Child => dialogControl ?? (dialogControl = new GeneralOptionsDialogControl());

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);

            dialogControl.ShowServerNuGetTrustWarning.IsChecked = Settings.ShowServerNuGetTrustWarning;
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            if (e.ApplyBehavior == ApplyKind.Apply)
            {
                Settings.ShowServerNuGetTrustWarning = dialogControl.ShowServerNuGetTrustWarning.IsChecked.Value;
            }

            base.OnApply(e);
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
    }
}
