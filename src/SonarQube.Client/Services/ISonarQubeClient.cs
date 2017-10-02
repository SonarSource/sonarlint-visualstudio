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

using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Messages;

namespace SonarQube.Client.Services
{
    public interface ISonarQubeClient
    {
        /// <summary>
        ///     Retrieves all components from the given SonarQube server.
        /// </summary>
        Task<Result<ComponentResponse[]>> GetComponentsSearchProjectsAsync(ComponentRequest request, CancellationToken token);

        /// <summary>
        ///     Retrieves all issues for the given project, module or file key.
        /// </summary>
        Task<Result<ServerIssue[]>> GetIssuesAsync(string key, CancellationToken token);

        /// <summary>
        ///     Retrieves all organizations from the given SonarQube server.
        /// </summary>
        Task<Result<OrganizationResponse[]>> GetOrganizationsAsync(OrganizationRequest request, CancellationToken token);

        /// <summary>
        ///     Retrieves all plugins installed on the given SonarQube server.
        /// </summary>
        /// <returns></returns>
        Task<Result<PluginResponse[]>> GetPluginsAsync(CancellationToken token);

        /// <summary>
        ///     Retrieves all the projects from the given SonarQube server.
        /// </summary>
        Task<Result<ProjectResponse[]>> GetProjectsAsync(CancellationToken token);

        /// <summary>
        ///     Retrieves all the properties for the given SonarQube server.
        /// </summary>
        Task<Result<PropertyResponse[]>> GetPropertiesAsync(CancellationToken token);

        /// <summary>
        ///     Retrieves the change log for the given quality profile.
        /// </summary>
        Task<Result<QualityProfileChangeLogResponse>> GetQualityProfileChangeLogAsync(QualityProfileChangeLogRequest request,
            CancellationToken token);

        /// <summary>
        ///     Retrieves the quality profile for the specified project and language.
        /// </summary>
        Task<Result<QualityProfileResponse[]>> GetQualityProfilesAsync(QualityProfileRequest request, CancellationToken token);

        /// <summary>
        ///     Retrieves the server's Roslyn Quality Profile export for the specified profile and language
        /// </summary>
        /// <remarks>
        ///     The export contains everything required to configure the solution to match the SonarQube server
        ///     analysis, including: the Code Analysis rule set, analyzer NuGet packages, and any other additional
        ///     files for the analyzers.
        /// </remarks>
        Task<Result<RoslynExportProfileResponse>> GetRoslynExportProfileAsync(RoslynExportProfileRequest request,
            CancellationToken token);

        /// <summary>
        ///     Retrieves the version of the given SonarQube server.
        /// </summary>
        Task<Result<VersionResponse>> GetVersionAsync(CancellationToken token);

        /// <summary>
        ///     Validates the given credentials on the given SonarQube server.
        /// </summary>
        Task<Result<CredentialResponse>> ValidateCredentialsAsync(CancellationToken token);

        /// <summary>
        ///     Gets list of notifications for the specified project from the given SonarQube server.
        /// </summary>
        Task<Result<NotificationsResponse[]>> GetNotificationEventsAsync(NotificationsRequest request,
            CancellationToken token);

    }
}