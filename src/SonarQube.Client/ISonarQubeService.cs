/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarQube.Client.Models;
using SonarQube.Client.Models.ServerSentEvents;

namespace SonarQube.Client;

public interface ISonarQubeService
{
    bool IsConnected { get; }

    /// <summary>
    ///     Returns <see cref="ServerInfo" /> that the service is connected to at this moment. Subsequent calls can result in different values.
    /// </summary>
    ServerInfo GetServerInfo();

    Task ConnectAsync(ConnectionInformation connection, CancellationToken token);

    void Disconnect();

    Task<IList<SonarQubeRule>> GetRulesAsync(
        bool isActive,
        string qualityProfileKey,
        CancellationToken token);

    /// <summary>
    ///     Returns all properties for the project with the specified projectKey. If a project with such
    ///     key does not exist, returns the default property values for the connected SonarQube server.
    /// </summary>
    Task<IList<SonarQubeProperty>> GetAllPropertiesAsync(string projectKey, CancellationToken token);

    /// <summary>
    ///     Wrapper for GET api/qualityprofiles/search
    /// </summary>
    /// <returns></returns>
    Task<IList<SonarQubeQualityProfile>> GetAllQualityProfilesAsync(
        string project,
        string organizationKey,
        CancellationToken token);

    /// <summary>
    ///     Returns the suppressed issues for the specified project/branch.
    /// </summary>
    /// <param name="projectKey">The project identifier</param>
    /// <param name="branch">
    ///     (optional) The Sonar branch for which issues should be returned. If null/empty,
    ///     the issues for the "main" branch will be returned.
    /// </param>
    /// <param name="token"></param>
    /// <param name="issueKeys">(optional) The ids of the issues to return. If empty, all issues will be returned.</param>
    Task<IList<SonarQubeIssue>> GetSuppressedRoslynIssuesAsync(
        string projectKey,
        string branch,
        string[] issueKeys,
        CancellationToken token);

    /// <summary>
    ///     Returns the issues in the specified server component with the same rule id
    /// </summary>
    /// <param name="projectKey">The project identifier</param>
    /// <param name="branch">
    ///     (optional) The Sonar branch for which issues should be returned. If null/empty,
    ///     the issues for the "main" branch will be returned
    /// </param>
    /// <param name="componentKey">The component identifier. Project/Directory/File</param>
    /// <param name="ruleId">The Rule identifier. Is used to limit the number of issues in the response</param>
    /// <param name="token"></param>
    Task<IList<SonarQubeIssue>> GetIssuesForComponentAsync(
        string projectKey,
        string branch,
        string componentKey,
        string ruleId,
        CancellationToken token);

    Task<IList<SonarQubeNotification>> GetNotificationEventsAsync(
        string projectKey,
        DateTimeOffset eventsSince,
        CancellationToken token);

    /// <summary>
    ///     Returns the list of server paths with matching file names
    /// </summary>
    /// <param name="projectKey">The project identifier</param>
    /// <param name="branch">
    ///     (optional) The Sonar branch for which issues should be returned. If null/empty,
    ///     the issues for the "main" branch will be returned
    /// </param>
    /// <param name="fileName">The file name used for the search</param>
    /// <param name="token"></param>
    Task<IList<string>> SearchFilesByNameAsync(
        string projectKey,
        string branch,
        string fileName,
        CancellationToken token);

    /// <summary>
    ///     Returns the URI to view the specified issue on the server
    /// </summary>
    /// <remarks>The method does not check whether the project or issue exists or not</remarks>
    Uri GetViewIssueUrl(string projectKey, string issueKey);

    /// <summary>
    ///     Returns branch information for the specified project key
    /// </summary>
    Task<IList<SonarQubeProjectBranch>> GetProjectBranchesAsync(string projectKey, CancellationToken token);

    /// <summary>
    ///     Returns the inclusions/exclusions
    /// </summary>
    Task<ServerExclusions> GetServerExclusions(string projectKey, CancellationToken token);

    /// <summary>
    ///     Creates a new <see cref="ISSEStreamReader" /> for the given <see cref="projectKey" />
    /// </summary>
    Task<ISSEStreamReader> CreateSSEStreamReader(string projectKey, CancellationToken token);
}
