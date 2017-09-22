/*
 * SonarQube Client
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Models;

namespace SonarQube.Client.Services
{
    public interface ISonarQubeService
    {
        bool HasOrganizationsFeature { get; }

        Task ConnectAsync(ConnectionInformation connection, CancellationToken token);

        Task<IList<Organization>> GetAllOrganizationsAsync(CancellationToken token);

        Task<IList<SonarQubePlugin>> GetAllPluginsAsync(CancellationToken token);

        Task<IList<SonarQubeProject>> GetAllProjectsAsync(string organizationKey, CancellationToken token);

        Task<IList<SonarQubeProperty>> GetAllPropertiesAsync(CancellationToken token);

        Uri GetProjectDashboardUrl(string projectKey);

        Task<QualityProfile> GetQualityProfileAsync(string projectKey, ServerLanguage language, CancellationToken token);

        Task<RoslynExportProfile> GetRoslynExportProfileAsync(string qualityProfileName, ServerLanguage language,
            CancellationToken token);
    }
}
