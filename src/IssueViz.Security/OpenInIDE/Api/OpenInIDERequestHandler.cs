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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE_Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api
{
    internal class OpenInIDERequestHandler
    {
        private readonly IIDEWindowService ideWindowService;
        private readonly IToolWindowService toolWindowService;
        private readonly ILocationNavigator navigator;
        private readonly IOpenInIDEHotspotsStore hotspotsStore;
        private readonly IOpenInIDEFailureInfoBar failureInfoBar;
        private readonly IIssueSelectionService issueSelectionService;
        private readonly ILogger logger;

        [ImportingConstructor]
        public OpenInIDERequestHandler(
            IIDEWindowService ideWindowService,
            IToolWindowService toolWindowService,
            ILocationNavigator navigator,
            IOpenInIDEHotspotsStore hotspotsStore,
            IOpenInIDEFailureInfoBar failureInfoBar,
            IIssueSelectionService issueSelectionService,
            ILogger logger)
        {
            this.ideWindowService = ideWindowService;
            this.toolWindowService = toolWindowService;
            this.navigator = navigator;
            this.hotspotsStore = hotspotsStore;
            this.failureInfoBar = failureInfoBar;
            this.issueSelectionService = issueSelectionService;
            this.logger = logger;
        }


    // todo: https://github.com/SonarSource/sonarlint-visualstudio/issues/5348
        
    //     private async Task ShowHotspotAsync()
    //     {
    //         // Always show the Hotspots tool window. If we can't successfully process the
    //         // request we'll show a gold bar in the window
    //         ideWindowService.BringToFront();
    //         toolWindowService.Show(OpenInIDEHotspotsToolWindow.ToolWindowId);
    //         await failureInfoBar.ClearAsync();
    //
    //         if (!ideStateValidator.CanHandleOpenInIDERequest())
    //         {
    //             // We're assuming the validator will have output an explanantion of why the IDE
    //             // isn't in the correct state
    //             await failureInfoBar.ShowAsync(OpenInIDEHotspotsToolWindow.ToolWindowId);
    //             return;
    //         }
    //         
    //         if (!navigator.TryNavigate(hotspotViz))
    //         {
    //             logger.WriteLine(OpenInIDEResources.ApiHandler_FailedToNavigateToHotspot, hotspotViz.FilePath, hotspotViz.StartLine);
    //         }
    //
    //         // Add to store and select regardless of whether navigation succeeded
    //         var addedHotspot = hotspotsStore.GetOrAdd(hotspotViz);
    //         issueSelectionService.SelectedIssue = addedHotspot;
    //     }
    }
}
