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
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.Secrets;
using SonarQube.Client;

namespace SonarLint.VisualStudio.CloudSecrets
{
    [Export(typeof(IConnectedModeSecrets))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ConnectedModeSecrets : IConnectedModeSecrets
    {
        private readonly ISonarQubeService sonarQubeService;
        private readonly Version MinimumRequiredSonarQubeVersion = new Version(9, 9);

        [ImportingConstructor]
        public ConnectedModeSecrets(ISonarQubeService sonarQubeService)
        {
            this.sonarQubeService = sonarQubeService;
        }

        public bool AreSecretsAvailable()
        {
            if (sonarQubeService.IsConnected)
            {
                var serverInfo = sonarQubeService.GetServerInfo();

                return serverInfo.ServerType == ServerType.SonarCloud ||
                      (serverInfo.ServerType == ServerType.SonarQube && serverInfo.Version >= MinimumRequiredSonarQubeVersion);
            }

            return false;
        }
    }
}
