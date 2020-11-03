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
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Contract;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api
{
    [Export(typeof(IOpenInIDERequestHandler))]
    internal class OpenInIDERequestHandler : IOpenInIDERequestHandler
    {
        private readonly IOpenInIDEStateValidator ideStateValidator;
        private readonly ISonarQubeService sonarQubeService;
        private readonly IHotspotToIssueVisualizationConverter converter;
        private readonly ILocationNavigator navigator;
        private readonly IHotspotsStore hotspotsStore;
        private readonly ILogger logger;

        [ImportingConstructor]
        public OpenInIDERequestHandler(IOpenInIDEStateValidator ideStateValidator,
            ISonarQubeService sonarQubeService,
            IHotspotToIssueVisualizationConverter converter,
            ILocationNavigator navigator,
            IHotspotsStore hotspotsStore,
            ILogger logger)
        {
            // MEF-created so the arguments should never be null
            this.ideStateValidator = ideStateValidator;
            this.sonarQubeService = sonarQubeService;
            this.converter = converter;
            this.navigator = navigator;
            this.hotspotsStore = hotspotsStore;
            this.logger = logger;
        }

        Task<IStatusResponse> IOpenInIDERequestHandler.GetStatusAsync()
        {
            throw new NotImplementedException();
        }

        async Task IOpenInIDERequestHandler.ShowHotspotAsync(IShowHotspotRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!ideStateValidator.CanHandleOpenInIDERequest(request.ServerUrl, request.ProjectKey, request.OrganizationKey))
            {
                return;
            }

            var hotspot = await TryGetHotspotData(request.HotspotKey);
            if (hotspot == null)
            {
                return;
            }

            var hotspotViz = converter.Convert(hotspot);
            if (hotspotViz == null)
            {
                return;
            }

            // TODO - show gold bar in event of failure to navigate. Also consider whether this class should
            // be responsible for showing the gold bar if the state validator returns false.
            if (!navigator.TryNavigate(hotspotViz))
            {
                logger.WriteLine(OpenInIDEResources.ApiHandler_FailedToNavigateToHotspot, hotspotViz.FilePath, hotspotViz.StartLine);
            }

            // Add to store regardless of whether navigation succeeded
            hotspotsStore.Add(hotspotViz);
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

    }
}
