/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core.Telemetry;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [ExcludeFromCodeCoverage] // https://github.com/SonarSource/sonarlint-visualstudio/issues/2760
    internal class OtherOptionsDialogPage : UIElementDialogPage
    {
        public const string PageName = "Other";

        private ITelemetryManager telemetryManager;

        private OtherOptionsDialogControl optionsDialogControl;
        protected override UIElement Child => optionsDialogControl ?? (optionsDialogControl = new OtherOptionsDialogControl());

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);

            var telemetryStatus = this.TelemetryManager?.GetStatus();
            SetEnabled(telemetryStatus);
            optionsDialogControl.ShareAnonymousData.IsChecked = telemetryStatus ?? false;
        }

        private void SetEnabled(bool? telemetryStatus)
        {
            optionsDialogControl.ShareAnonymousData.IsEnabled = telemetryStatus.HasValue;
            optionsDialogControl.ShareAnonymousData.Foreground = telemetryStatus.HasValue ? Brushes.Black : Brushes.Gray;
            optionsDialogControl.BackendStartedText.Visibility = telemetryStatus.HasValue ? Visibility.Hidden : Visibility.Visible;
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            if (e.ApplyBehavior == ApplyKind.Apply &&
                this.TelemetryManager != null)
            {
                var wasShared = this.TelemetryManager.GetStatus() ?? false;
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
