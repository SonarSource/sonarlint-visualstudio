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

using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class OtherOptionsDialogPage : UIElementDialogPage
    {
        public const string PageName = "Other";

        private ITelemetryManager telemetryManager;

        private OtherOptionsDialogControl optionsDialogControl;
        protected override UIElement Child => optionsDialogControl ?? (optionsDialogControl = new OtherOptionsDialogControl());

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);

            this.optionsDialogControl.ShareAnonymousData.IsChecked = this.TelemetryManager?.IsAnonymousDataShared ?? false;
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            if (e.ApplyBehavior == ApplyKind.Apply &&
                this.TelemetryManager != null)
            {
                var wasShared = this.TelemetryManager.IsAnonymousDataShared;
                var isShared = this.optionsDialogControl.ShareAnonymousData.IsChecked ?? true;

                if (wasShared && !isShared)
                {
                    this.TelemetryManager.OptOut();
                }
                else if (!wasShared && isShared)
                {
                    this.TelemetryManager.OptIn();
                }
            }

            base.OnApply(e);
        }

        private ITelemetryManager TelemetryManager
        {
            get
            {
                if (this.telemetryManager == null)
                {
                    Debug.Assert(this.Site != null, "Expecting the page to be sited");
                    this.telemetryManager = this.Site.GetMefService<ITelemetryManager>();
                }
                return this.telemetryManager;
            }
        }
    }
}
