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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Messages;
using SonarQube.Client.Models;
using SonarQube.Client.Models.ServerSentEvents;

namespace SonarQube.Client
{
    public interface ISonarQubeService
    {
        /// <summary>
        /// Returns <see cref="ServerInfo"/> that the service is connected to at this moment. Subsequent calls can result in different values.
        /// </summary>
        ServerInfo GetServerInfo();

        /// <summary>
        /// Returns whether organizations are available and being used
        /// on the server
        /// </summary>
        Task<bool> HasOrganizations(CancellationToken token);

        bool IsConnected { get; }

        Task ConnectAsync(ConnectionInformation connection, CancellationToken token);

        void Disconnect();

        Task<IList<SonarQubeOrganization>> GetAllOrganizationsAsync(CancellationToken token);

        Task<IList<SonarQubeRule>> GetRulesAsync(bool isActive, string qualityProfileKey,
            CancellationToken token);

        /// <summary>
        /// Returns SonarQubeRule based on the key. If rule not found returns null
        /// </summary>
        /// <param name="ruleKey">Composite rule key eg: roslyn.sonaranalyzer.security.cs:S5135 </param>
        /// <param name="qualityProfileKey">Alphanumeric key of the Quality Profile eg: AYbqXI3oMyTRqYlqVUCf </param>
        Task<SonarQubeRule> GetRuleByKeyAsync(string ruleKey, string qualityProfileKey, CancellationToken token);

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

        /// <summary>
        /// Wrapper for GET api/qualityprofiles/search
        /// </summary>
        /// <returns></returns>
        Task<IList<SonarQubeQualityProfile>> GetAllQualityProfilesAsync(string project, string organizationKey,
            CancellationToken token);

        Task<SonarQubeQualityProfile> GetQualityProfileAsync(string projectKey, string organizationKey,
            SonarQubeLanguage language, CancellationToken token);

        Task<RoslynExportProfileResponse> GetRoslynExportProfileAsync(string qualityProfileName, string organizationKey,
            SonarQubeLanguage language, CancellationToken token);

        /// <summary>
        /// Returns the suppressed issues for the specified project/branch.
        /// </summary>
        /// <param name="projectKey">The project identifier</param>
        /// <param name="branch">(optional) The Sonar branch for which issues should be returned. If null/empty,
        ///     the issues for the "main" branch will be returned.</param>
        /// <param name="token"></param>
        /// <param name="issueKeys">(optional) The ids of the issues to return. If empty, all issues will be returned.</param>
        Task<IList<SonarQubeIssue>> GetSuppressedIssuesAsync(string projectKey, string branch, string[] issueKeys, CancellationToken token);

        Task<IList<SonarQubeNotification>> GetNotificationEventsAsync(string projectKey,
            DateTimeOffset eventsSince, CancellationToken token);

        Task<IList<SonarQubeModule>> GetAllModulesAsync(string projectKey, CancellationToken token);

        Task<SonarQubeHotspot> GetHotspotAsync(string hotspotKey, CancellationToken token);

        /// <summary>
        /// Returns the taint issues for the specified project/branch.
        /// </summary>
        /// <param name="projectKey">The project identifier</param>
        /// <param name="branch">(optional) The Sonar branch for which taint issues should be returned. If null/empty,
        /// the issues for the "main" branch will be returned.</param>
        Task<IList<SonarQubeIssue>> GetTaintVulnerabilitiesAsync(string projectKey, string branch, CancellationToken token);

        /// <summary>
        /// Returns the URI to view the specified issue on the server
        /// </summary>
        /// <remarks>The method does not check whether the project or issue exists or not</remarks>
        Uri GetViewIssueUrl(string projectKey, string issueKey);

        /// <summary>
        /// Returns the URI to view the specified hotspot on the server
        /// </summary>
        /// <remarks>The method does not check whether the project or hotspot exists or not</remarks>
        Uri GetViewHotspotUrl(string projectKey, string hotspotKey);

        /// <summary>
        /// Returns the source code for the specified file
        /// </summary>
        /// <param name="fileKey">e.g. my_project:src/foo/Bar.php</param>
        Task<string> GetSourceCodeAsync(string fileKey, CancellationToken token);

        /// <summary>
        /// Returns branch information for the specified project key
        /// </summary>
        Task<IList<SonarQubeProjectBranch>> GetProjectBranchesAsync(string projectKey, CancellationToken token);

        /// <summary>
        /// Returns the inclusions/exclusions
        /// </summary>
        Task<ServerExclusions> GetServerExclusions(string projectKey, CancellationToken token);

        /// <summary>
        /// Creates a new <see cref="ISSEStreamReader"/> for the given <see cref="projectKey"/>
        /// </summary>
        Task<ISSEStreamReader> CreateSSEStreamReader(string projectKey, CancellationToken token);

        Task<IList<SonarQubeHotspotSearch>> SearchHotspotsAsync(string projectKey, string branch, CancellationToken token);

        /// <summary>
        /// Sets review status for an issue and adds an optional comment.
        /// See api/issues/do_transition and api/issues/add_comment
        /// </summary>
        Task TransitionIssue(string issueKey, SonarQubeIssueTransition transition, string optionalComment, CancellationToken token);
    }
}
