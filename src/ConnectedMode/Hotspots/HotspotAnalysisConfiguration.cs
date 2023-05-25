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

namespace SonarLint.VisualStudio.ConnectedMode.Hotspots
{
    public interface IHotspotAnalysisConfiguration
    {
        bool IsEnabled();
    }

    [Export(typeof(IHotspotAnalysisConfiguration))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class HotspotAnalysisConfiguration : IHotspotAnalysisConfiguration
    {
        private readonly Version minimalSonarQubeVersion = new Version(9, 7);
        private readonly ISonarQubeService sonarQubeService;

        [ImportingConstructor]
        public HotspotAnalysisConfiguration(ISonarQubeService sonarQubeService)
        {
            this.sonarQubeService = sonarQubeService;
        }

        public bool IsEnabled()
        {
            var serverInfo = sonarQubeService.GetServerInfo();

            return serverInfo != null 
                   && (serverInfo.ServerType == ServerType.SonarCloud 
                       || (serverInfo.ServerType == ServerType.SonarQube && serverInfo.Version >= minimalSonarQubeVersion));
        }
    }
}
