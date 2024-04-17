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

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.OpenInIDE;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList
{
    [Guid(IssueListIds.HotspotsIdAsString)]
    public class HotspotsToolWindow : ToolWindowPane
    {
        public static readonly Guid ToolWindowId = IssueListIds.HotspotsId;

        public HotspotsToolWindow(IServiceProvider serviceProvider)
        {
            Caption = Resources.HotspotsToolWindowCaption;

            var componentModel = serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;

            var store = componentModel.GetService<ILocalHotspotsStore>();
            var locationNavigator = componentModel.GetService<ILocationNavigator>();
            var selectionService = componentModel.GetService<IIssueSelectionService>();
            var threadHandling = componentModel.GetService<IThreadHandling>();
            var navigateToRuleDescriptionCommand = componentModel.GetService<INavigateToRuleDescriptionCommand>();

            var viewModel = new HotspotsControlViewModel(store, navigateToRuleDescriptionCommand, locationNavigator, selectionService, threadHandling);
            viewModel.UpdateHotspotsListAsync().Forget();
            var hotspotsControl = new HotspotsControl(viewModel);

            Content = hotspotsControl;
        }
    }
}
