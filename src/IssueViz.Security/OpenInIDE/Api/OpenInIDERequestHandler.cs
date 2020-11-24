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
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Contract;
using SonarLint.VisualStudio.IssueVisualization.Security.SelectionService;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api
{
    [Export(typeof(IOpenInIDERequestHandler))]
    internal class OpenInIDERequestHandler : IOpenInIDERequestHandler
    {
        private readonly IIDEWindowService ideWindowService;
        private readonly IToolWindowService toolWindowService;
        private readonly IOpenInIDEStateValidator ideStateValidator;
        private readonly ISonarQubeService sonarQubeService;
        private readonly IHotspotToIssueVisualizationConverter converter;
        private readonly ILocationNavigator navigator;
        private readonly IHotspotsStore hotspotsStore;
        private readonly IOpenInIDEFailureInfoBar failureInfoBar;
        private readonly IHotspotsSelectionService hotspotsSelectionService;
        private readonly ITelemetryManager telemetryManager;
        private readonly ILogger logger;

        [ImportingConstructor]
        public OpenInIDERequestHandler(
            IIDEWindowService ideWindowService,
            IToolWindowService toolWindowService,
            IOpenInIDEStateValidator ideStateValidator,
            ISonarQubeService sonarQubeService,
            IHotspotToIssueVisualizationConverter converter,
            ILocationNavigator navigator,
            IHotspotsStore hotspotsStore,
            IOpenInIDEFailureInfoBar failureInfoBar,
            IHotspotsSelectionService hotspotsSelectionService,
            ITelemetryManager telemetryManager,
            ILogger logger)
        {
            // MEF-created so the arguments should never be null
            this.ideWindowService = ideWindowService;
            this.toolWindowService = toolWindowService;
            this.ideStateValidator = ideStateValidator;
            this.sonarQubeService = sonarQubeService;
            this.converter = converter;
            this.navigator = navigator;
            this.hotspotsStore = hotspotsStore;
            this.failureInfoBar = failureInfoBar;
            this.hotspotsSelectionService = hotspotsSelectionService;
            this.telemetryManager = telemetryManager;
            this.logger = logger;
        }

        Task IOpenInIDERequestHandler.ShowHotspotAsync(IShowHotspotRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return ShowHotspotAsync(request);
        }

        private async Task ShowHotspotAsync(IShowHotspotRequest request)
        {
            logger.WriteLine(OpenInIDEResources.ApiHandler_ProcessingRequest, request.ServerUrl, request.ProjectKey,
                request.OrganizationKey ?? OpenInIDEResources.ApiHandler_NullOrganization, request.HotspotKey);

            // Always show the Hotspots tool window. If we can't successfully process the
            // request we'll show a gold bar in the window
            telemetryManager.ShowHotspotRequested();
            ideWindowService.BringToFront();
            toolWindowService.Show(HotspotsToolWindow.ToolWindowId);
            await failureInfoBar.ClearAsync();

            if (!ideStateValidator.CanHandleOpenInIDERequest(request.ServerUrl, request.ProjectKey, request.OrganizationKey))
            {
                // We're assuming the validator will have output an explanantion of why the IDE
                // isn't in the correct state
                await failureInfoBar.ShowAsync(HotspotsToolWindow.ToolWindowId);
                return;
            }

            var hotspot = await TryGetHotspotData(request.HotspotKey);
            if (hotspot == null)
            {
                await failureInfoBar.ShowAsync(HotspotsToolWindow.ToolWindowId);
                return;
            }

            var hotspotViz = TryCreateIssueViz(hotspot);
            if (hotspotViz == null)
            {
                await failureInfoBar.ShowAsync(HotspotsToolWindow.ToolWindowId);
                return;
            }

            if (!navigator.TryNavigate(hotspotViz))
            {
                logger.WriteLine(OpenInIDEResources.ApiHandler_FailedToNavigateToHotspot, hotspotViz.FilePath, hotspotViz.StartLine);
                await failureInfoBar.ShowAsync(HotspotsToolWindow.ToolWindowId);
            }

            // Add to store and select regardless of whether navigation succeeded
            hotspotsStore.Add(hotspotViz);
            hotspotsSelectionService.Select(hotspotViz);
        }

        private async Task<SonarQubeHotspot> TryGetHotspotData(string hotspotKey)
        {
            // We're calling an external system so exceptions might occur e.g. network errors
            try
            {
                return await sonarQubeService.GetHotspotAsync(hotspotKey, CancellationToken.None);
            }
            catch(Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(OpenInIDEResources.ApiHandler_FailedToFetchHotspot, ex.Message);
            }
            return null;
        }

        private IAnalysisIssueVisualization TryCreateIssueViz(SonarQubeHotspot hotspot)
        {
            try
            {
                return converter.Convert(hotspot);
            }
            catch(Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(OpenInIDEResources.ApiHandler_UnableToConvertHotspotData, ex.Message);
            }
            return null;
        }
    }
}
