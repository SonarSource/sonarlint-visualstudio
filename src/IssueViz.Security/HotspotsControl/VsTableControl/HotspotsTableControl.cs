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

using System.Windows.Controls;
using WpfTableControlProvider = versionSpecificShell::Microsoft.Internal.VisualStudio.Shell.TableControl.IWpfTableControlProvider;
using WpfTableControl = versionSpecificShellFramework::Microsoft.VisualStudio.Shell.TableControl.IWpfTableControl;
using TableManagerProvider = versionSpecificShellFramework::Microsoft.VisualStudio.Shell.TableManager.ITableManagerProvider;
using TableManager = versionSpecificShellFramework::Microsoft.VisualStudio.Shell.TableManager.ITableManager;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsControl.VsTableControl
{
    internal class HotspotsTableControl
    {
        private readonly HotspotsTableDataSource dataSource;
        private readonly TableManager tableManager;

        public WpfTableControl TableControl { get; }

        public HotspotsTableControl(TableManagerProvider tableManagerProvider, WpfTableControlProvider wpfTableControlProvider)
        {
            dataSource = new HotspotsTableDataSource();

            tableManager = tableManagerProvider.GetTableManager(HotspotsTableConstants.TableManagerIdentifier);
            tableManager.AddSource(dataSource, HotspotsTableColumns.Names);

            TableControl = wpfTableControlProvider.CreateControl(tableManager,
                true,
                HotspotsTableColumns.InitialStates,
                HotspotsTableColumns.Names);

            TableControl.SelectionMode = SelectionMode.Single;
        }

        public void Dispose()
        {
            TableControl?.Dispose();
            tableManager?.RemoveSource(dataSource);
            dataSource?.Dispose();
        }
    }
}
