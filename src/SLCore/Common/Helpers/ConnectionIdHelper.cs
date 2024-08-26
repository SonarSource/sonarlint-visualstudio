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
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.SLCore.Common.Helpers
{
    public interface IConnectionIdHelper
    {
        Uri GetUriFromConnectionId(string connectionId);

        string GetConnectionIdFromServerConnection(ServerConnection serverConnection);
    }

    [Export(typeof(IConnectionIdHelper))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ConnectionIdHelper : IConnectionIdHelper
    {
        private const string SonarCloudPrefix = "sc|";
        private const string SonarQubePrefix = "sq|";
        public static readonly Uri SonarCloudUri = new Uri("https://sonarcloud.io");

        public string GetConnectionIdFromServerConnection(ServerConnection serverConnection) =>
            serverConnection switch
            { // todo create jira task to remove this prefix
                ServerConnection.SonarQube sonarQube => SonarQubePrefix + sonarQube.Id,
                ServerConnection.SonarCloud sonarCloud => SonarCloudPrefix + sonarCloud.Id,
                _ => null
            };

        public Uri GetUriFromConnectionId(string connectionId)
        {
            if (connectionId == null)
            {
                return null;
            }

            if (connectionId.StartsWith(SonarCloudPrefix))
            {
                var uriString = connectionId.Substring(SonarCloudPrefix.Length);

                return !string.IsNullOrWhiteSpace(uriString) ? SonarCloudUri : null;
            }

            if (connectionId.StartsWith(SonarQubePrefix))
            {
                return Uri.TryCreate(connectionId.Substring(SonarQubePrefix.Length), UriKind.Absolute, out var uri)
                    ? uri
                    : null;
            }

            return null;
        }
    }
}
