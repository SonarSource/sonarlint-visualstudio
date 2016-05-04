//-----------------------------------------------------------------------
// <copyright file="ConfigurableSonarQubeServiceWrapper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.Service.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSonarQubeServiceWrapper : ISonarQubeServiceWrapper
    {
        private int connectRequestsCount;
        private readonly IDictionary<string, IDictionary<string, Uri>> projectDashboardUrls = new Dictionary<string, IDictionary<string, Uri>>();

        #region Testing helpers
        public bool AllowConnections { get; set; } = true;

        public ProjectInformation[] ReturnProjectInformation { get; set; }

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
            this.connectRequestsCount = 0;
        }

        public void AssertConnectRequests(int expectedCount)
        {
            Assert.AreEqual(expectedCount, this.connectRequestsCount, "Connect was not called the expected number of times");
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
            Assert.IsNotNull(connection, "The API requires a connection information");

            if (this.ExpectedConnection != null)
            {
                Assert.AreEqual(this.ExpectedConnection?.ServerUri, connection.ServerUri, "The connection is not as expected");
            }
        }

        private void AssertExpectedProjectInformation(ProjectInformation projectInformation)
        {
            Assert.IsNotNull(projectInformation, "The API requires project information");

            if (this.ExpectedProjectKey != null)
            {
                Assert.AreEqual(this.ExpectedProjectKey, projectInformation.Key, "Unexpected project key");
            }
        }
        #endregion

        #region ISonarQubeServiceWrapper


        bool ISonarQubeServiceWrapper.TryGetProjects(ConnectionInformation serverConnection, CancellationToken token, out ProjectInformation[] serverProjects)
        {
            this.AssertExpectedConnection(serverConnection);
            this.connectRequestsCount++;

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

            Assert.IsNotNull(profile, "QualityProfile is expected");

            this.GetExportAction?.Invoke();

            export = null;
            this.ReturnExport.TryGetValue(profile, out export);

            QualityProfile profile2;
            this.ReturnProfile.TryGetValue(language, out profile2);
            Assert.AreSame(profile2, profile, "Unexpected profile for language");

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

        #endregion
    }
}
