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
using System.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSonarQubeServiceWrapper : ISonarQubeServiceWrapper
    {
        private ConnectionInformation connection;
        private int connectRequestsCount;
        private int disconnectRequestsCount;
        private readonly IDictionary<string, IDictionary<string, Uri>> projectDashboardUrls = new Dictionary<string, IDictionary<string, Uri>>();

        #region Testing helpers
        public bool AllowConnections { get; set; } = true;

        public ProjectInformation[] ReturnProjectInformation { get; set; }

        public ISet<ServerPlugin> ServerPlugins { get; } = new HashSet<ServerPlugin>();

        public ISet<ServerProperty> ServerProperties { get; } = new HashSet<ServerProperty>();

        /// <summary>
        /// Language to rules map
        /// </summary>
        public Dictionary<string, RoslynExportProfile> ReturnExport { get; } = new Dictionary<string, RoslynExportProfile>();

        public Action GetExportAction { get; set; }

        public void ResetCounters()
        {
            this.connectRequestsCount = 0;
            this.disconnectRequestsCount = 0;
        }

        public void AssertConnectRequests(int expectedCount)
        {
            Assert.AreEqual(expectedCount, this.connectRequestsCount, "Connect was not called the expected number of times");
        }

        public void AssertDisconnectRequests(int expectedCount)
        {
            Assert.AreEqual(expectedCount, this.disconnectRequestsCount, "Disconnect was not called the expected number of times");
        }

        public void SetConnection(Uri serverUri)
        {
            this.SetConnection(new ConnectionInformation(serverUri));
        }

        public void SetConnection(ConnectionInformation connectionInformation)
        {
            this.connection = connectionInformation;
        }

        public void ClearConnection()
        {
            this.connection = null;
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

        #endregion

        #region ISonarQubeServiceWrapper

        ConnectionInformation ISonarQubeServiceWrapper.CurrentConnection
        {
            get
            {
                return this.connection;
            }
        }

        IEnumerable<ProjectInformation> ISonarQubeServiceWrapper.Connect(ConnectionInformation connectionInformation, CancellationToken token)
        {
            Assert.IsNotNull(connectionInformation, "Not expected a null as the argument");
            this.connectRequestsCount++;

            if (this.AllowConnections && !token.IsCancellationRequested)
            {
                this.connection = connectionInformation;
                return this.ReturnProjectInformation;
            }
            else
            {
                this.connection = null;
                return null;
            }
        }

        void ISonarQubeServiceWrapper.Disconnect()
        {
            this.disconnectRequestsCount++;
            this.connection = null;
        }


        RoslynExportProfile ISonarQubeServiceWrapper.GetExportProfile(ProjectInformation project, string language, CancellationToken token)
        {
            Assert.IsNotNull(project, "ProjectInformation is expected");

            this.GetExportAction?.Invoke();

            RoslynExportProfile export = null;
            this.ReturnExport?.TryGetValue(language, out export);
            return export;
        }

        public IEnumerable<ServerPlugin> GetPlugins(ConnectionInformation connectionInformation, CancellationToken token)
        {
            return this.ServerPlugins;
        }

        public IEnumerable<ServerProperty> GetProperties(CancellationToken token)
        {
            return this.ServerProperties;
        }

        public Uri CreateProjectDashboardUrl(ConnectionInformation connectionInformation, ProjectInformation project)
        {
            Uri url;
            IDictionary<string, Uri> projects;
            if (this.projectDashboardUrls.TryGetValue(connectionInformation.ServerUri.ToString(), out projects) 
                && projects.TryGetValue(project.Key, out url))
            {
                return url;
            }
            return null;
        }

        #endregion
    }
}
