﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList;
using SonarQube.Client;
using VSShell = Microsoft.VisualStudio.Shell;
using VSShellInterop = Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint
{
    internal interface ITaintIssuesSynchronizer
    {
        /// <summary>
        /// Fetches taint vulnerabilities from the server, converts them into visualizations and populates <see cref="ITaintStore"/>.
        /// </summary>
        Task SynchronizeWithServer();
    }

    [Export(typeof(ITaintIssuesSynchronizer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class TaintIssuesSynchronizer : ITaintIssuesSynchronizer
    {
        private static readonly Version MinimumRequiredSonarQubeVersion = new Version(8, 6);

        private readonly ITaintStore taintStore;
        private readonly ISonarQubeService sonarQubeService;
        private readonly ITaintIssueToIssueVisualizationConverter converter;
        private readonly IConfigurationProvider configurationProvider;
        private readonly IToolWindowService toolWindowService;
        private readonly ILogger logger;

        private readonly VSShellInterop.IVsMonitorSelection vsMonitorSelection;
        private readonly uint contextCookie;

        [ImportingConstructor]
        public TaintIssuesSynchronizer(ITaintStore taintStore,
            ISonarQubeService sonarQubeService,
            ITaintIssueToIssueVisualizationConverter converter,
            IConfigurationProvider configurationProvider,
            IToolWindowService toolWindowService,
            [Import(typeof(VSShell.SVsServiceProvider))] IServiceProvider serviceProvider,
            ILogger logger)
        {
            this.taintStore = taintStore;
            this.sonarQubeService = sonarQubeService;
            this.converter = converter;
            this.configurationProvider = configurationProvider;
            this.toolWindowService = toolWindowService;
            this.logger = logger;

            vsMonitorSelection = (VSShellInterop.IVsMonitorSelection)serviceProvider.GetService(typeof(VSShellInterop.SVsShellMonitorSelection));
            Guid localGuid = TaintIssuesExistUIContext.Guid;
            vsMonitorSelection.GetCmdUIContextCookie(ref localGuid, out contextCookie);
        }

        public async Task SynchronizeWithServer()
        {
            try
            {
                var bindingConfiguration = configurationProvider.GetConfiguration();

                if (IsStandalone(bindingConfiguration) || !IsConnected() || !IsFeatureSupported())
                {
                    HandleNoTaintIssues();
                    return;
                }

                var projectKey = bindingConfiguration.Project.ProjectKey;
                var taintVulnerabilities = await sonarQubeService.GetTaintVulnerabilitiesAsync(projectKey, CancellationToken.None);

                logger.WriteLine(TaintResources.Synchronizer_NumberOfServerIssues, taintVulnerabilities.Count);

                var hasTaintIssues = taintVulnerabilities.Count > 0;

                if (!hasTaintIssues)
                {
                    HandleNoTaintIssues();
                }
                else
                {
                    var analysisInformation = await GetAnalysisInformation(projectKey);
                    var taintIssueVizs = taintVulnerabilities.Select(converter.Convert).ToArray();
                    taintStore.Set(taintIssueVizs, analysisInformation);

                    UpdateTaintIssuesUIContext(true);

                    // We need the tool window content to exist so the issues are filtered and the
                    // tool window caption is updated. See the "EnsureToolWindowExists" method comment
                    // for more information.
                    toolWindowService.EnsureToolWindowExists(TaintToolWindow.ToolWindowId);
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(TaintResources.Synchronizer_Failure, ex);
                HandleNoTaintIssues();
            }
        }

        private bool IsStandalone(BindingConfiguration bindingConfiguration)
        {
            if (bindingConfiguration.Mode == SonarLintMode.Standalone)
            {
                logger.WriteLine(TaintResources.Synchronizer_NotInConnectedMode);
                return true;
            }

            return false;
        }

        private bool IsConnected()
        {
            if (sonarQubeService.IsConnected)
            {
                return true;
            }

            logger.WriteLine(TaintResources.Synchronizer_ServerNotConnected);
            return false;
        }

        private bool IsFeatureSupported()
        {
            if (sonarQubeService.ServerInfo.ServerType == ServerType.SonarCloud ||
                sonarQubeService.ServerInfo.Version >= MinimumRequiredSonarQubeVersion)
            {
                return true;
            }

            logger.WriteLine(TaintResources.Synchronizer_UnsupportedSQVersion, sonarQubeService.ServerInfo.Version);
            return false;
        }

        private async Task<AnalysisInformation> GetAnalysisInformation(string projectKey)
        {
            var branches = await sonarQubeService.GetProjectBranchesAsync(projectKey, CancellationToken.None);
            var issuesBranch = branches.First(x => x.IsMain);

            return new AnalysisInformation(issuesBranch.Name, issuesBranch.LastAnalysisTimestamp);
        }

        private void HandleNoTaintIssues()
        {
            ClearStore();
            UpdateTaintIssuesUIContext(false);
        }

        private void ClearStore()
        {
            taintStore.Set(Enumerable.Empty<IAnalysisIssueVisualization>(), null);
        }

        private void UpdateTaintIssuesUIContext(bool hasTaintIssues) =>
            vsMonitorSelection.SetCmdUIContext(contextCookie, hasTaintIssues ? 1 : 0);
    }
}
