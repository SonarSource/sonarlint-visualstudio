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
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList;

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api
{
    internal interface IOpenInIDEStateValidator : IDisposable
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

    [Export(typeof(IOpenInIDEStateValidator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class OpenInIdeStateValidator : IOpenInIDEStateValidator
    {
        private readonly IInfoBarManager infoBarManager;
        private readonly IConfigurationProvider configurationProvider;
        private readonly ILogger logger;
        private IInfoBar currentInfoBar;

        [ImportingConstructor]
        public OpenInIdeStateValidator(IInfoBarManager infoBarManager, IConfigurationProvider configurationProvider, ILogger logger)
        {
            this.infoBarManager = infoBarManager;
            this.configurationProvider = configurationProvider;
            this.logger = logger;
        }

        public bool CanHandleOpenInIDERequest(Uri serverUri, string projectKey, string organizationKey)
        {
            RemoveExistingInfoBar();

            var failureMessage = GetFailureMessage(serverUri, projectKey, organizationKey);

            if (string.IsNullOrEmpty(failureMessage))
            {
                return true;
            }

            AddInfoBar(failureMessage);
            logger.WriteLine(failureMessage);

            return false;
        }

        private string GetFailureMessage(Uri serverUri, string projectKey, string organizationKey)
        {
            var configuration = configurationProvider.GetConfiguration();
            string reason = null;

            if (configuration.Mode == SonarLintMode.Standalone)
            {
                reason = OpenInIDEResources.Infobar_InvalidStateReason_NotInConnectedMode;
            }
            else if (!configuration.Project.ServerUri.Equals(serverUri))
            {
                reason = string.Format(OpenInIDEResources.Infobar_InvalidStateReason_WrongServer, configuration.Project.ServerUri);
            }
            else if (!string.IsNullOrEmpty(organizationKey) &&
                (configuration.Project.Organization == null ||
                 !organizationKey.Equals(configuration.Project.Organization.Key, StringComparison.OrdinalIgnoreCase)))
            {
                reason = string.Format(OpenInIDEResources.Infobar_InvalidStateReason_WrongOrganization, configuration.Project.Organization?.Key);
            }
            else if (!configuration.Project.ProjectKey.Equals(projectKey, StringComparison.OrdinalIgnoreCase))
            {
                reason = string.Format(OpenInIDEResources.Infobar_InvalidStateReason_WrongProject, configuration.Project.ProjectKey);
            }

            if (reason == null)
            {
                return null;
            }

            var instructions = string.IsNullOrEmpty(organizationKey)
                ? OpenInIDEResources.Inforbar_Instructions_SonarQube
                : OpenInIDEResources.Inforbar_Instructions_SonarCloud;

            var fullMessage = string.Format(OpenInIDEResources.Inforbar_InvalidState, reason, instructions);

            return fullMessage;
        }

        private void AddInfoBar(string fullMessage)
        {
            currentInfoBar = infoBarManager.AttachInfoBar(new Guid(HotspotsToolWindow.Guid), fullMessage, default);
            currentInfoBar.Closed += CurrentInfoBar_Closed;
        }

        private void RemoveExistingInfoBar()
        {
            if (currentInfoBar != null)
            {
                currentInfoBar.Closed -= CurrentInfoBar_Closed;
                infoBarManager.DetachInfoBar(currentInfoBar);
                currentInfoBar = null;
            }
        }

        private void CurrentInfoBar_Closed(object sender, EventArgs e)
        {
            RemoveExistingInfoBar();
        }

        public void Dispose()
        {
            RemoveExistingInfoBar();
        }
    }
}
