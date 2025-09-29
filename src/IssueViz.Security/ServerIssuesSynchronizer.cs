/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.DependencyRisks;
using SonarLint.VisualStudio.SLCore.Service.Taint;
using VSShellInterop = Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.IssueVisualization.Security;

internal interface IServerIssuesSynchronizer
{
    /// <summary>
    ///     Fetches issues (e.g. taint vulnerabilities, dependency risks) from the server, converts them into models and populates corresponding store
    /// </summary>
    Task UpdateServerIssuesAsync(ConfigurationScope configurationScope);
}

[Export(typeof(IServerIssuesSynchronizer))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class ServerIssuesSynchronizer : IServerIssuesSynchronizer
{
    private readonly IAsyncLock asyncLock;
    private readonly ITaintStore taintStore;
    private readonly IDependencyRisksStore dependencyRisksStore;
    private readonly ISLCoreServiceProvider slCoreServiceProvider;
    private readonly ITaintIssueToIssueVisualizationConverter taintConverter;
    private readonly IToolWindowService toolWindowService;
    private readonly IVsUIServiceOperation vSServiceOperation;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger generalLogger;
    private readonly ILogger taintLogger;
    private readonly ILogger dependencyRiskLogger;
    private readonly IScaIssueDtoToDependencyRiskConverter scaConverter;

    [method: ImportingConstructor]
    public ServerIssuesSynchronizer(
        ITaintStore taintStore,
        IDependencyRisksStore dependencyRisksStore,
        ISLCoreServiceProvider slCoreServiceProvider,
        ITaintIssueToIssueVisualizationConverter taintConverter,
        IToolWindowService toolWindowService,
        IVsUIServiceOperation vSServiceOperation,
        IThreadHandling threadHandling,
        IAsyncLockFactory asyncLockFactory,
        ILogger logger,
        IScaIssueDtoToDependencyRiskConverter scaConverter)
    {
        this.taintStore = taintStore;
        this.dependencyRisksStore = dependencyRisksStore;
        this.slCoreServiceProvider = slCoreServiceProvider;
        this.taintConverter = taintConverter;
        this.toolWindowService = toolWindowService;
        this.vSServiceOperation = vSServiceOperation;
        this.threadHandling = threadHandling;
        generalLogger = logger.ForContext(Resources.Synchronizer_LogContext_General).ForVerboseContext(nameof(ServerIssuesSynchronizer));
        taintLogger = generalLogger.ForContext(Resources.Synchronizer_LogContext_Taint);
        dependencyRiskLogger = generalLogger.ForContext(Resources.LogContext_DependencyRisks);
        this.scaConverter = scaConverter;
        asyncLock = asyncLockFactory.Create();
    }

    public Task UpdateServerIssuesAsync(ConfigurationScope configurationScope) =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            using (await asyncLock.AcquireAsync())
            {
                await PerformSynchronizationInternalAsync(configurationScope);
            }
        });

    private async Task PerformSynchronizationInternalAsync(ConfigurationScope configurationScope)
    {
        if (!IsConnectedModeConfigScope(configurationScope)
            || !IsConfigScopeReady(configurationScope))
        {
            HandleNoTaintIssues();
            HandleNoScaIssues();
            return;
        }

        await UpdateTaintsAsync(configurationScope);
        await UpdateScaAsync(configurationScope);
    }

    private async Task UpdateTaintsAsync(ConfigurationScope configurationScope)
    {
        try
        {
            if (!TryGetSlCoreService(out ITaintVulnerabilityTrackingSlCoreService taintService, taintLogger))
            {
                HandleNoTaintIssues();
                return;
            }

            if (IsAlreadyInitializedForConfigScope(configurationScope, taintStore.ConfigurationScope, taintLogger))
            {
                return;
            }

            var taintsResponse = await taintService.ListAllAsync(new ListAllTaintsParams(configurationScope.Id, true));
            taintLogger.WriteLine(Resources.Synchronizer_NumberOfTaintIssues, taintsResponse.taintVulnerabilities.Count);

            taintStore.Set(taintsResponse.taintVulnerabilities.Select(x => taintConverter.Convert(x, configurationScope.RootPath)).ToArray(), configurationScope.Id);

            HandleUIContextUpdate(taintsResponse.taintVulnerabilities.Count);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            taintLogger.WriteLine(Resources.Synchronizer_Failure, ex);
            HandleNoTaintIssues();
        }
    }

    private async Task UpdateScaAsync(ConfigurationScope configurationScope)
    {
        try
        {
            if (!TryGetSlCoreService(out IDependencyRiskSlCoreService scaService, dependencyRiskLogger))
            {
                HandleNoScaIssues();
                return;
            }

            if (IsAlreadyInitializedForConfigScope(configurationScope, dependencyRisksStore.CurrentConfigurationScope, dependencyRiskLogger))
            {
                return;
            }

            var scaResponse = await scaService.ListAllAsync(new ListAllDependencyRisksParams(configurationScope.Id));
            dependencyRiskLogger.WriteLine(Resources.Synchronizer_NumberOfDependencyRisks, scaResponse.dependencyRisks.Count);

            var dependencyRisks = scaResponse.dependencyRisks.Select(x => scaConverter.Convert(x)).ToArray();
            dependencyRisksStore.Set(dependencyRisks, configurationScope.Id);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            dependencyRiskLogger.WriteLine(Resources.Synchronizer_Failure, ex);
            HandleNoScaIssues();
        }
    }

    private bool TryGetSlCoreService<T>(out T service, ILogger logger) where T : class, ISLCoreService
    {
        var result = slCoreServiceProvider.TryGetTransientService(out service);
        if (!result)
        {
            logger.WriteLine(Resources.Synchronizer_SLCoreNotReady);
        }
        return result;
    }

    private bool IsConfigScopeReady(ConfigurationScope configurationScope)
    {
        var isReady = configurationScope.RootPath is not null;
        if (!isReady)
        {
            generalLogger.LogVerbose(Resources.Synchronizer_Verbose_ConfigScopeNotReady);
        }
        return isReady;
    }

    private static bool IsAlreadyInitializedForConfigScope(ConfigurationScope configurationScope, string currentStoreConfigurationScope, ILogger logger)
    {
        var isAlreadyInitialized = currentStoreConfigurationScope == configurationScope.Id;
        if (isAlreadyInitialized)
        {
            logger.LogVerbose(Resources.Synchronizer_Verbose_AlreadyInitialized);
        }
        return isAlreadyInitialized;
    }

    private void HandleUIContextUpdate(int taintsCount)
    {
        if (taintsCount > 0)
        {
            UpdateTaintIssuesUIContext(true);

            // We need the tool window content to exist so the issues are filtered and the
            // tool window caption is updated. See the "EnsureToolWindowExists" method comment
            // for more information.
            toolWindowService.EnsureToolWindowExists(ReportViewToolWindow.ToolWindowId);
        }
        else
        {
            UpdateTaintIssuesUIContext(false);
        }
    }

    private bool IsConnectedModeConfigScope(ConfigurationScope configurationScope)
    {
        if (configurationScope is { SonarProjectId: not null })
        {
            return true;
        }

        generalLogger.WriteLine(Resources.Synchronizer_NotInConnectedMode);
        return false;
    }

    private void HandleNoTaintIssues()
    {
        taintStore.Reset();
        UpdateTaintIssuesUIContext(false);
    }

    private void HandleNoScaIssues()
    {
        dependencyRisksStore.Reset();
    }

    private void UpdateTaintIssuesUIContext(bool hasTaintIssues) =>
        vSServiceOperation.Execute<VSShellInterop.SVsShellMonitorSelection, VSShellInterop.IVsMonitorSelection>(monitorSelection =>
        {
            var localGuid = ReportViewIssuesExistUIContext.Guid;

            monitorSelection.GetCmdUIContextCookie(ref localGuid, out var cookie);
            monitorSelection.SetCmdUIContext(cookie, hasTaintIssues ? 1 : 0);
        });
}
