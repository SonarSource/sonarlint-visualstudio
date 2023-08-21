/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarQube.Client;

namespace SonarLint.VisualStudio.Core.Configuration
{
    /// <summary>
    /// Contains version specific feature configuration
    /// </summary>
    public interface IConnectedModeFeaturesConfiguration
    {
        /// <summary>
        /// Indicates whether Local Hotspot Analysis is supported in the current Connected Mode state  
        /// </summary>
        /// <returns>True if connected to SCloud or SQube 9.7 and above, False otherwise</returns>
        bool IsHotspotsAnalysisEnabled();
        /// <summary>
        /// Indicates whether the new Clean Code Taxonomy should be used in the current Connected Mode state
        /// </summary>
        /// <returns>False if connected to SQube 10.1.X and below, True otherwise (including Standalone)</returns>
        bool IsNewCctAvailable();
    }

    [Export(typeof(IConnectedModeFeaturesConfiguration))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ConnectedModeFeaturesConfiguration : IConnectedModeFeaturesConfiguration
    {
        private readonly Version minimalSonarQubeVersionForHotspots = new Version(9, 7);
        private readonly Version minimalSonarQubeVersionForNewTaxonomy = new Version(10, 2);
        private readonly ISonarQubeService sonarQubeService;

        [ImportingConstructor]
        public ConnectedModeFeaturesConfiguration(ISonarQubeService sonarQubeService)
        {
            this.sonarQubeService = sonarQubeService;
        }

        public bool IsNewCctAvailable()
        {
            var serverInfo = sonarQubeService.GetServerInfo();
            
            // use new cct in standalone, connected to SC or connected to SQ >=10.2
            return serverInfo == null || IsSupportedForVersion(serverInfo, minimalSonarQubeVersionForNewTaxonomy);
        }
        
        public bool IsHotspotsAnalysisEnabled()
        {
            var serverInfo = sonarQubeService.GetServerInfo();
            
            // analyze hotspots connected to SC or connected to SQ >= 9.7
            return serverInfo != null && IsSupportedForVersion(serverInfo, minimalSonarQubeVersionForHotspots);
        }

        private static bool IsSupportedForVersion(ServerInfo serverInfo, Version minimumVersion) =>
            serverInfo.ServerType == ServerType.SonarCloud
            || (serverInfo.ServerType == ServerType.SonarQube &&
                serverInfo.Version >= minimumVersion);
    }
}
