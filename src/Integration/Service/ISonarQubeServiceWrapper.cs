/*
 * SonarLint for Visual Studio
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
using System.Threading;
using SonarLint.VisualStudio.Integration.Service.DataModel;

namespace SonarLint.VisualStudio.Integration.Service
{
    /// <summary>
    /// SonarQube service abstraction for testing purposes
    /// </summary>
    internal interface ISonarQubeServiceWrapper
    {
        /// <summary>
        /// Retrieves all the server projects
        /// </summary>
        bool TryGetProjects(ConnectionInformation serverConnection, CancellationToken token, out ProjectInformation[] serverProjects);

        /// <summary>
        /// Retrieves all the server properties.
        /// </summary>
        bool TryGetProperties(ConnectionInformation serverConnection, CancellationToken token, out ServerProperty[] properties);

        /// <summary>
        /// Retrieves the server's Roslyn Quality Profile export for the specified profile and language
        /// </summary>
        /// <remarks>
        /// The export contains everything required to configure the solution to match the SonarQube server analysis,
        /// including: the Code Analysis rule set, analyzer NuGet packages, and any other additional files for the analyzers.
        /// </remarks>
        /// <param name="profile">Quality profile. Required.</param>
        /// <param name="language">Language scope. Required.</param>
        bool TryGetExportProfile(ConnectionInformation serverConnection, QualityProfile profile, Language language, CancellationToken token, out RoslynExportProfile export);

        /// <summary>
        /// Retrieves all server plugins
        /// </summary>
        /// <returns>All server plugins, or null on connection failure</returns>
        bool TryGetPlugins(ConnectionInformation serverConnection, CancellationToken token, out ServerPlugin[] plugins);

        /// <summary>
        /// Retrieves the quality profile information for the specified project and language
        /// </summary>
        /// <param name="project">Required project information for which to retrieve the export</param>
        /// <param name="language">Language scope. Required.</param>
        bool TryGetQualityProfile(ConnectionInformation serverConnection, ProjectInformation project, Language language, CancellationToken token, out QualityProfile profile);

        /// <summary>
        /// Generate a <see cref="Uri"/> to the dashboard for the given project on the provided <paramref name="serverConnection"/>.
        /// </summary>
        /// <param name="project">Project to generate the <see cref="Uri"/> for</param>
        Uri CreateProjectDashboardUrl(ConnectionInformation serverConnection, ProjectInformation project);
    }
}
