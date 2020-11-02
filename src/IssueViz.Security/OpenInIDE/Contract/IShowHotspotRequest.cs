/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Contract
{
    /// <summary>
    /// Describes a request from the server to open a hotspot
    /// </summary>
    /// <remarks>
    /// As a minumum, the request needs to contain enough information for SonarLint to
    /// determine whether an open solution is connected to the correct SonarQube/SonarCloud
    /// server and project.
    /// </remarks>
    public interface IShowHotspotRequest
    {
        /// <summary>
        /// The URL of the requesting server. Required.
        /// </summary>
        System.Uri ServerUrl { get; }

        /// <summary>
        /// The organization of the key. Optional - SonarCloud only.
        /// </summary>
        string OrganizationKey { get; }

        /// <summary>
        /// The server project key. Required.
        /// </summary>
        string ProjectKey { get; }

        /// <summary>
        /// The key of the hotspot to show. Required.
        /// </summary>
        string HotspotKey { get; }
    }
}
