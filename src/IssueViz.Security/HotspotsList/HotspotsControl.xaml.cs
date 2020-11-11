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
using System.Linq;
using System.Windows.Controls;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource;
using SonarLint.VisualStudio.IssueVisualization.Security.SelectionService;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList
{
    internal sealed partial class HotspotsControl : UserControl, IDisposable
    {
        private readonly IHotspotsSelectionService selectionService;
        private readonly IWpfTableControl wpfTableControl;

        public HotspotsControl(ITableManagerProvider tableManagerProvider, 
            IWpfTableControlProvider wpfTableControlProvider,
            IHotspotsSelectionService selectionService)
        {
            this.selectionService = selectionService;
            InitializeComponent();

            wpfTableControl = CreateWpfTableControl(tableManagerProvider, wpfTableControlProvider);
            hotspotsList.Child = wpfTableControl.Control;

            selectionService.SelectionChanged += SelectionService_SelectionChanged;
        }

        private static IWpfTableControl CreateWpfTableControl(ITableManagerProvider tableManagerProvider, IWpfTableControlProvider wpfTableControlProvider)
        {
            var tableManager = tableManagerProvider.GetTableManager(HotspotsTableConstants.TableManagerIdentifier);

            var wpfTableControl = wpfTableControlProvider.CreateControl(tableManager,
                true,
                HotspotsTableColumns.InitialStates,
                HotspotsTableColumns.Names);

            wpfTableControl.SelectionMode = SelectionMode.Single;

            return wpfTableControl;
        }

        private async void SelectionService_SelectionChanged(object sender, SelectionService.SelectionChangedEventArgs e)
        {
            if (e.SelectedHotspot == null)
            {
                return;
            }

            await wpfTableControl.ForceUpdateAsync();

            await RunOnUIThread.RunAsync(() =>
            {
                var tableEntryHandle = wpfTableControl.Entries.FirstOrDefault(x => x.Identity == e.SelectedHotspot);

                if (tableEntryHandle != null)
                {
                    wpfTableControl.SelectedEntries = new[] {tableEntryHandle};
                }
            });
        }

        public void Dispose()
        {
            selectionService.SelectionChanged -= SelectionService_SelectionChanged;

            wpfTableControl?.Dispose();
        }
    }
}
