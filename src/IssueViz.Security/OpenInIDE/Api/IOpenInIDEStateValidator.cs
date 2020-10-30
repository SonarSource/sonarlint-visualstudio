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

using System;

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api
{
    internal interface IOpenInIDEStateValidator
    {
        /// <summary>
        /// Checks whether the IDE is in the correct status to handle an "Open in IDE" request
        /// for the specified server and project.
        /// </summary>
        /// <param name="serverUri">The URL of the target SonarQube/SonarCloud server (required)</param>
        /// <param name="projectKey">The key of the target project (required)</param>
        /// <param name="organizationKey">The key of the target organization (null for SonarQube servers, required for SonarCloud)</param>
        /// <returns>True if the IDE is in connected mode with a project bound to the specified server/project/organization, otherwise false</returns>
        /// <remarks>
        /// For an Open in IDE request to succeed:
        /// * a solution must be open
        /// * the solution must be in connected mode, pointing to the correct server/project/organization.
        ///
        /// The validator is responsible for handling any UX notifications if the IDE is not in an appropriate state.
        /// </remarks>
        bool CanHandleOpenInIDERequest(Uri serverUri, string projectKey, string organizationKey);
    }
}
