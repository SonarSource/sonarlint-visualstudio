﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.ReviewHotspot;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList
{
    [Guid(ToolWindowIdAsString)]
    public class HotspotsToolWindow : ToolWindowPane
    {
        private const string ToolWindowIdAsString = "4BCD4392-DBCF-4AA2-9852-01129D229CD8";
        public static readonly Guid ToolWindowId = new Guid(ToolWindowIdAsString);

        public HotspotsToolWindow(IServiceProvider serviceProvider)
        {
            Caption = Resources.HotspotsToolWindowCaption;

            var componentModel = serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;

            var store = componentModel.GetService<ILocalHotspotsStore>();
            var locationNavigator = componentModel.GetService<ILocationNavigator>();
            var selectionService = componentModel.GetService<IIssueSelectionService>();
            var threadHandling = componentModel.GetService<IThreadHandling>();
            var navigateToRuleDescriptionCommand = componentModel.GetService<INavigateToRuleDescriptionCommand>();
            var activeSolutionBoundTracker = componentModel.GetService<IActiveSolutionBoundTracker>();
            var reviewHotspotsService = componentModel.GetService<IReviewHotspotsService>();
            var messageBox = componentModel.GetService<IMessageBox>();

            var viewModel = new HotspotsControlViewModel(store, navigateToRuleDescriptionCommand, locationNavigator, selectionService, threadHandling, activeSolutionBoundTracker,
                reviewHotspotsService, messageBox);
            viewModel.UpdateHotspotsListAsync().Forget();
            var hotspotsControl = new HotspotsControl(viewModel);

            Content = hotspotsControl;
        }
    }
}
