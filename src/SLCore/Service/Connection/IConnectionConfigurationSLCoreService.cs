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

using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Protocol;

namespace SonarLint.VisualStudio.SLCore.Service.Connection;

[JsonRpcClass("connection")]
public interface IConnectionConfigurationSLCoreService : ISLCoreService
{
    /// <summary>
    /// Changes Connection Configuration
    /// </summary>
    /// <param name="parameters"></param>
    void DidUpdateConnections(DidUpdateConnectionsParams parameters);

    /// <summary>
    /// Connection credentials have been changed
    /// </summary>
    /// <param name="parameters"></param>
    void DidChangeCredentials(DidChangeCredentialsParams parameters);

    /// <summary>
    ///  Validate that connection is valid:
    ///  - check that the server is reachable
    ///  - check that the server minimal version is satisfied
    ///  - check that the credentials are valid
    ///  - check that the organization exists (for SonarCloud)
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    Task<ValidateConnectionResponse> ValidateConnectionAsync(ValidateConnectionParams parameters);

    Task<ListUserOrganizationsResponse> ListUserOrganizationsAsync(ListUserOrganizationsParams parameters);
    
    /// <summary>
    /// Fuzzy search among Sonar projects existing on SonarQube or in a SonarCloud organization.
    /// </summary>
    /// <param name="parameters"></param>
    Task<FuzzySearchProjectsResponse> FuzzySearchProjectsAsync(FuzzySearchProjectsParams parameters);
    
    /// <summary>
    /// Get all Sonar projects existing on SonarQube or in a SonarCloud organization.
    /// As this data might be needed during connection creation, it accepts a transient connection.
    /// The number of returned projects is limited to 10000.
    /// </summary>
    /// <param name="parameters"></param>
    Task<GetAllProjectsResponse> GetAllProjectsAsync(GetAllProjectsParams parameters);

    /// <summary>
    /// Returns a map of project names by project key; project name is null if it wasn't found
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    Task<GetProjectNamesByKeyResponse> GetProjectNamesByKeyAsync(GetProjectNamesByKeyParams parameters);
}
