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

using System;
using System.Windows.Controls;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList
{
    internal sealed partial class HotspotsControl : UserControl, IDisposable
    {
        private readonly IWpfTableControl wpfTableControl;

        public HotspotsControl(ITableManagerProvider tableManagerProvider, IWpfTableControlProvider wpfTableControlProvider)
        {
            InitializeComponent();

            wpfTableControl = CreateWpfTableControl(tableManagerProvider, wpfTableControlProvider);

            hotspotsList.Child = wpfTableControl.Control;
        }

        private static IWpfTableControl CreateWpfTableControl(ITableManagerProvider tableManagerProvider, IWpfTableControlProvider wpfTableControlProvider)
        {
            var tableManager = tableManagerProvider.GetTableManager(HotspotsTableConstants.TableManagerIdentifier);

            var wpfTableControl = wpfTableControlProvider.CreateControl(tableManager,
                true,
                HotspotsTableColumns.InitialStates,
                HotspotsTableColumns.Names) as IWpfTableControl2;

            wpfTableControl.NavigationBehavior = TableEntryNavigationBehavior.AcceptsDoubleClick |
                                                 TableEntryNavigationBehavior.AcceptsEnter;

            wpfTableControl.SelectionMode = SelectionMode.Single;

            return wpfTableControl;
        }

        public void Dispose()
        {
            wpfTableControl?.Dispose();
        }
    }
}
