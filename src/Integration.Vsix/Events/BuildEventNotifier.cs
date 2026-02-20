/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.IssueVisualization.Security.Issues;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.Integration.Vsix.Events;

[Export(typeof(IBuildEventNotifier))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class BuildEventNotifier : IBuildEventNotifier, IVsUpdateSolutionEvents
{
    private readonly ILocalIssuesStore localIssuesStore;
    private readonly IBuildEventUiManager buildEventUIManager;
    private readonly IToolWindowService toolWindowService;
    private readonly IVsUIServiceOperation vsUIServiceOperation;
    private readonly ILogger logger;
    private bool isDisposed;
    private uint cookie;

    [ImportingConstructor]
    public BuildEventNotifier(
        ILocalIssuesStore localIssuesStore,
        IBuildEventUiManager buildEventUIManager,
        IToolWindowService toolWindowService,
        IInitializationProcessorFactory initializationProcessorFactory,
        IVsUIServiceOperation vsUIServiceOperation,
        ILogger logger)
    {
        this.localIssuesStore = localIssuesStore;
        this.buildEventUIManager = buildEventUIManager;
        this.toolWindowService = toolWindowService;
        this.vsUIServiceOperation = vsUIServiceOperation;
        this.logger = logger.ForContext(Strings.BuildEventNotifier_LogContext);
        InitializationProcessor = initializationProcessorFactory.CreateAndStart<BuildEventNotifier>([], InitializeInternalAsync);
    }

    public IInitializationProcessor InitializationProcessor { get; }

    private async Task InitializeInternalAsync()
    {
        if (isDisposed)
        {
            return;
        }
        await vsUIServiceOperation.ExecuteAsync<SVsSolutionBuildManager, IVsSolutionBuildManager2>(buildManager =>
        {
            ErrorHandler.ThrowOnFailure(buildManager.AdviseUpdateSolutionEvents(this, out cookie));
        });
    }

    int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate) => VSConstants.S_OK;

    int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
    {
        try
        {
            var issues = localIssuesStore.GetAll();
            var errorCount = issues.Count(i => i.VsSeverity == __VSERRORCATEGORY.EC_ERROR);
            if (errorCount > 0)
            {
                var result = buildEventUIManager.ShowErrorNotificationDialog(errorCount);
                if (result)
                {
                    toolWindowService.Show(IssueListIds.ErrorListId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.WriteLine(ex.ToString());
        }

        return VSConstants.S_OK;
    }

    int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate) => VSConstants.S_OK;

    int IVsUpdateSolutionEvents.UpdateSolution_Cancel() => VSConstants.S_OK;

    int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => VSConstants.S_OK;

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        if (InitializationProcessor.IsFinalized)
        {
            vsUIServiceOperation.Execute<SVsSolutionBuildManager, IVsSolutionBuildManager2>(buildManager =>
            {
                buildManager.UnadviseUpdateSolutionEvents(cookie);
            });
        }

        isDisposed = true;
    }
}
