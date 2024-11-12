﻿/*
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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Taint;
using SonarLint.VisualStudio.SLCore.State;
using VSShellInterop = Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint;

internal interface ITaintIssuesSynchronizer
{
    /// <summary>
    ///     Fetches taint vulnerabilities from the server, converts them into visualizations and populates <see cref="ITaintStore" />.
    /// </summary>
    Task UpdateTaintVulnerabilitiesAsync(ConfigurationScope configurationScope);
}

[Export(typeof(ITaintIssuesSynchronizer))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class TaintIssuesSynchronizer : ITaintIssuesSynchronizer
{
    private readonly IAsyncLock asyncLock;
    private readonly ITaintIssueToIssueVisualizationConverter converter;
    private readonly ILogger logger;
    private readonly ISLCoreServiceProvider slCoreServiceProvider;
    private readonly ITaintStore taintStore;
    private readonly IThreadHandling threadHandling;
    private readonly IToolWindowService toolWindowService;
    private readonly IVsUIServiceOperation vSServiceOperation;

    [ImportingConstructor]
    public TaintIssuesSynchronizer(
        ITaintStore taintStore,
        ISLCoreServiceProvider slCoreServiceProvider,
        ITaintIssueToIssueVisualizationConverter converter,
        IToolWindowService toolWindowService,
        IVsUIServiceOperation vSServiceOperation,
        IThreadHandling threadHandling,
        IAsyncLockFactory asyncLockFactory,
        ILogger logger)
    {
        this.taintStore = taintStore;
        this.slCoreServiceProvider = slCoreServiceProvider;
        this.converter = converter;
        this.toolWindowService = toolWindowService;
        this.vSServiceOperation = vSServiceOperation;
        asyncLock = asyncLockFactory.Create();
        this.threadHandling = threadHandling;
        this.logger = logger;
    }

    public Task UpdateTaintVulnerabilitiesAsync(ConfigurationScope configurationScope) =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            using (await asyncLock.AcquireAsync())
            {
                await PerformSynchronizationInternalAsync(configurationScope);
            }
        });

    private async Task PerformSynchronizationInternalAsync(ConfigurationScope configurationScope)
    {
        try
        {
            if (IsStandalone(configurationScope) || !slCoreServiceProvider.TryGetTransientService(out ITaintVulnerabilityTrackingSlCoreService taintService))
            {
                HandleNoTaintIssues();
                return;
            }

            if (!IsConfigScopeReady(configurationScope) || IsAlreadyInitializedForConfigScope(configurationScope))
            {
                return;
            }

            var taintsResponse = await taintService.ListAllAsync(new ListAllTaintsParams(configurationScope.Id, true));
            logger.WriteLine(TaintResources.Synchronizer_NumberOfServerIssues, taintsResponse.taintVulnerabilities.Count);

            taintStore.Set(taintsResponse.taintVulnerabilities.Select(x => converter.Convert(x, configurationScope.RootPath)).ToArray(), configurationScope.Id);

            HandleUIContextUpdate(taintsResponse);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(TaintResources.Synchronizer_Failure, ex);
            HandleNoTaintIssues();
        }
    }

    private static bool IsConfigScopeReady(ConfigurationScope configurationScope) => configurationScope.RootPath is not null;

    private bool IsAlreadyInitializedForConfigScope(ConfigurationScope configurationScope) => taintStore.ConfigurationScope == configurationScope.Id;

    private void HandleUIContextUpdate(ListAllTaintsResponse taintsResponse)
    {
        var hasTaintIssues = taintsResponse.taintVulnerabilities.Count > 0;

        if (!hasTaintIssues)
        {
            UpdateTaintIssuesUIContext(false);
        }
        else
        {
            UpdateTaintIssuesUIContext(true);

            // We need the tool window content to exist so the issues are filtered and the
            // tool window caption is updated. See the "EnsureToolWindowExists" method comment
            // for more information.
            toolWindowService.EnsureToolWindowExists(TaintToolWindow.ToolWindowId);
        }
    }

    private bool IsStandalone(ConfigurationScope configurationScope)
    {
        if (configurationScope is { SonarProjectId: not null })
        {
            return false;
        }

        logger.WriteLine(TaintResources.Synchronizer_NotInConnectedMode);
        return true;
    }

    private void HandleNoTaintIssues()
    {
        ClearStore();
        UpdateTaintIssuesUIContext(false);
    }

    private void ClearStore() => taintStore.Set([], null);

    private void UpdateTaintIssuesUIContext(bool hasTaintIssues) =>
        vSServiceOperation.Execute<VSShellInterop.SVsShellMonitorSelection, VSShellInterop.IVsMonitorSelection>(
            monitorSelection =>
            {
                var localGuid = TaintIssuesExistUIContext.Guid;

                monitorSelection.GetCmdUIContextCookie(ref localGuid, out var cookie);
                monitorSelection.SetCmdUIContext(cookie, hasTaintIssues ? 1 : 0);
            });
}
