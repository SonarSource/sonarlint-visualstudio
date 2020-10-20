/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

extern alias versionSpecificShell;
extern alias versionSpecificShellFramework;

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsControl.VsTableControl;
using WpfTableControlProvider = versionSpecificShell::Microsoft.Internal.VisualStudio.Shell.TableControl.IWpfTableControlProvider;
using TableManagerProvider = versionSpecificShellFramework::Microsoft.VisualStudio.Shell.TableManager.ITableManagerProvider;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsControl
{
    [Guid("4BCD4392-DBCF-4AA2-9852-01129D229CD8")]
    public class HotspotsToolWindow : ToolWindowPane
    {
        private readonly HotspotsTableControl hotspotsTableControl;

        public HotspotsToolWindow(IServiceProvider serviceProvider) : base(null)
        {
            Caption = Resources.HotspotsToolWindowCaption;

            var componentModel = serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            var tableManagerProvider = componentModel.GetService<TableManagerProvider>();
            var wpfTableControlProvider = componentModel.GetService<WpfTableControlProvider>();
            hotspotsTableControl = new HotspotsTableControl(tableManagerProvider, wpfTableControlProvider);

            Content = new HotspotsControl(hotspotsTableControl.TableControl.Control);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                hotspotsTableControl?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
