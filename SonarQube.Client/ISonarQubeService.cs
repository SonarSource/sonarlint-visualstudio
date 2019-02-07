/*
 * SonarQube Client
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Messages;
using SonarQube.Client.Models;

namespace SonarQube.Client
{
    public interface ISonarQubeService
    {
        bool HasOrganizationsFeature { get; }
        bool IsConnected { get; }

        Task ConnectAsync(ConnectionInformation connection, CancellationToken token);

        void Disconnect();

        Task<IList<SonarQubeOrganization>> GetAllOrganizationsAsync(CancellationToken token);

        Task<IList<SonarQubeRule>> GetRulesAsync(bool isActive, string qualityProfileKey,
            CancellationToken token);

        Task<IList<SonarQubeLanguage>> GetAllLanguagesAsync(CancellationToken token);

        Task<Stream> DownloadStaticFileAsync(string pluginKey, string fileName, CancellationToken token);

        Task<IList<SonarQubePlugin>> GetAllPluginsAsync(CancellationToken token);

        Task<IList<SonarQubeProject>> GetAllProjectsAsync(string organizationKey, CancellationToken token);

        /// <summary>
        /// Returns all properties for the project with the specified projectKey. If a project with such
        /// key does not exist, returns the default property values for the connected SonarQube server.
        /// </summary>
        Task<IList<SonarQubeProperty>> GetAllPropertiesAsync(string projectKey, CancellationToken token);

        Uri GetProjectDashboardUrl(string projectKey);

        Task<SonarQubeQualityProfile> GetQualityProfileAsync(string projectKey, string organizationKey,
            SonarQubeLanguage language, CancellationToken token);

        Task<RoslynExportProfileResponse> GetRoslynExportProfileAsync(string qualityProfileName, string organizationKey,
            SonarQubeLanguage language, CancellationToken token);

        Task<IList<SonarQubeIssue>> GetSuppressedIssuesAsync(string projectKey, CancellationToken token);

        Task<IList<SonarQubeNotification>> GetNotificationEventsAsync(string projectKey,
            DateTimeOffset eventsSince, CancellationToken token);

        Task<IList<SonarQubeModule>> GetAllModulesAsync(string projectKey, CancellationToken token);
    }
}
