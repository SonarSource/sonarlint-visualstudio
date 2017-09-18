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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.Service.DataModel;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSonarQubeServiceWrapper : ISonarQubeServiceWrapper
    {
        internal int ConnectionRequestsCount { get; private set; }
        private readonly IDictionary<string, IDictionary<string, Uri>> projectDashboardUrls = new Dictionary<string, IDictionary<string, Uri>>();

        #region Testing helpers

        public bool AllowConnections { get; set; } = true;

        public bool HasOrganizationsSupport { get; set; }
        public bool AreCredentialsValid { get; set; } = true;
        public bool AreNotificationsSupported { get; set; }

        public ProjectInformation[] ReturnProjectInformation { get; set; }
        public NotificationEvent[] ReturnNotificationEvents { get; set; }
        public OrganizationInformation[] ReturnOrganizationInformation { get; set; }

        public ISet<ServerPlugin> ServerPlugins { get; } = new HashSet<ServerPlugin>();

        public ISet<ServerProperty> ServerProperties { get; } = new HashSet<ServerProperty>();

        /// <summary>
        /// QualityProfile to export map
        /// </summary>
        public Dictionary<QualityProfile, RoslynExportProfile> ReturnExport { get; } = new Dictionary<QualityProfile, RoslynExportProfile>();

        /// Language to quality profile map
        /// </summary>
        public Dictionary<Language, QualityProfile> ReturnProfile { get; } = new Dictionary<Language, QualityProfile>();

        public Action GetExportAction { get; set; }

        public void ResetCounters()
        {
            this.ConnectionRequestsCount = 0;
        }

        public void RegisterServerPlugin(ServerPlugin plugin)
        {
            this.ServerPlugins.Add(plugin);
        }

        public void ClearServerPlugins()
        {
            this.ServerPlugins.Clear();
        }

        public void RegisterServerProperty(ServerProperty property)
        {
            this.ServerProperties.Add(property);
        }

        public void ClearServerProperties()
        {
            this.ServerProperties.Clear();
        }

        public void RegisterProjectDashboardUrl(ConnectionInformation connectionInfo, ProjectInformation projectInfo, Uri url)
        {
            var serverUrl = connectionInfo.ServerUri.ToString();
            var projectKey = projectInfo.Key;
            if (!this.projectDashboardUrls.ContainsKey(serverUrl))
            {
                this.projectDashboardUrls[serverUrl] = new Dictionary<string, Uri>();
            }

            this.projectDashboardUrls[serverUrl][projectKey] = url;
        }

        public ConnectionInformation ExpectedConnection
        {
            get;
            set;
        }

        public string ExpectedProjectKey
        {
            get;
            set;
        }

        private void AssertExpectedConnection(ConnectionInformation connection)
        {
            connection.Should().NotBeNull("The API requires a connection information");

            if (this.ExpectedConnection != null)
            {
                connection.ServerUri.Should().Be(this.ExpectedConnection?.ServerUri, "The connection is not as expected");
            }
        }

        private void AssertExpectedProjectInformation(ProjectInformation projectInformation)
        {
            projectInformation.Should().NotBeNull("The API requires project information");

            if (this.ExpectedProjectKey != null)
            {
                projectInformation.Key.Should().Be(this.ExpectedProjectKey, "Unexpected project key");
            }
        }

        #endregion Testing helpers

        #region ISonarQubeServiceWrapper

        bool ISonarQubeServiceWrapper.TryGetOrganizations(ConnectionInformation serverConnection, CancellationToken token, out OrganizationInformation[] organizations)
        {
            this.AssertExpectedConnection(serverConnection);
            this.ConnectionRequestsCount++;

            if (this.AllowConnections && !token.IsCancellationRequested)
            {
                organizations = this.ReturnOrganizationInformation;
                return true;
            }
            else
            {
                organizations = null;
                return false;
            }
        }

        bool ISonarQubeServiceWrapper.TryGetProjects(ConnectionInformation serverConnection, CancellationToken token, out ProjectInformation[] serverProjects)
        {
            this.AssertExpectedConnection(serverConnection);
            this.ConnectionRequestsCount++;

            if (this.AllowConnections && !token.IsCancellationRequested)
            {
                serverProjects = this.ReturnProjectInformation;
                return true;
            }
            else
            {
                serverProjects = null;
                return false;
            }
        }

        bool ISonarQubeServiceWrapper.TryGetExportProfile(ConnectionInformation serverConnection, QualityProfile profile, Language language, CancellationToken token, out RoslynExportProfile export)
        {
            this.AssertExpectedConnection(serverConnection);

            profile.Should().NotBeNull("QualityProfile is expected");

            this.GetExportAction?.Invoke();

            export = null;
            this.ReturnExport.TryGetValue(profile, out export);

            QualityProfile profile2;
            this.ReturnProfile.TryGetValue(language, out profile2);
            profile.Should().Be(profile2, "Unexpected profile for language");

            return export != null;
        }

        bool ISonarQubeServiceWrapper.TryGetPlugins(ConnectionInformation serverConnection, CancellationToken token, out ServerPlugin[] plugins)
        {
            this.AssertExpectedConnection(serverConnection);

            plugins = this.ServerPlugins.ToArray();

            return true;
        }

        bool ISonarQubeServiceWrapper.TryGetProperties(ConnectionInformation serverConnection, CancellationToken token, out ServerProperty[] properties)
        {
            this.AssertExpectedConnection(serverConnection);

            properties = this.ServerProperties.ToArray();

            return true;
        }

        Uri ISonarQubeServiceWrapper.CreateProjectDashboardUrl(ConnectionInformation serverConnection, ProjectInformation project)
        {
            this.AssertExpectedConnection(serverConnection);

            Uri url;
            IDictionary<string, Uri> projects;
            if (this.projectDashboardUrls.TryGetValue(serverConnection.ServerUri.ToString(), out projects)
                && projects.TryGetValue(project.Key, out url))
            {
                return url;
            }
            return null;
        }

        bool ISonarQubeServiceWrapper.TryGetQualityProfile(ConnectionInformation serverConnection, ProjectInformation project, Language language, CancellationToken token, out QualityProfile profile)
        {
            profile = null;

            if (this.AllowConnections && !token.IsCancellationRequested)
            {
                this.AssertExpectedConnection(serverConnection);

                this.AssertExpectedProjectInformation(project);

                this.ReturnProfile.TryGetValue(language, out profile);
            }

            return profile != null;
        }

        bool ISonarQubeServiceWrapper.AreCredentialsValid(ConnectionInformation serverConnection, CancellationToken token)
        {
            this.AssertExpectedConnection(serverConnection);

            return AreCredentialsValid;
        }

        bool ISonarQubeServiceWrapper.HasOrganizationsSupport(ConnectionInformation serverConnection, CancellationToken token)
        {
            this.AssertExpectedConnection(serverConnection);

            return HasOrganizationsSupport;
        }

        public bool HasNotificationSupport(ConnectionInformation serverConnection, CancellationToken token)
        {
            this.AssertExpectedConnection(serverConnection);
            return AreNotificationsSupported;
        }

        public bool TryGetNotificationEvents(ConnectionInformation serverConnection, CancellationToken token, ProjectInformation project, DateTimeOffset eventsSince, out NotificationEvent[] events)
        {
            this.AssertExpectedConnection(serverConnection);
            events = ReturnNotificationEvents;
            return true;
        }

        #endregion ISonarQubeServiceWrapper
    }
}