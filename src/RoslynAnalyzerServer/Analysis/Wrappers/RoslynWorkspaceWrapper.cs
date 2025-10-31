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
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

[ExcludeFromCodeCoverage] // todo SLVS-2466 add roslyn 'integration' tests using AdHocWorkspace
[Export(typeof(IRoslynWorkspaceWrapper))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class RoslynWorkspaceWrapper : IRoslynWorkspaceWrapper
{
    private readonly ILogger quickFixApplicationLogger;
    private readonly Workspace workspace;
    private readonly IAnalysisRequester analysisRequester;
    private readonly IThreadHandling threadHandling;
    private readonly IWorkspaceChangeIndicator workspaceChangeIndicator;
    private bool disposed;

    [method: ImportingConstructor]
    public RoslynWorkspaceWrapper(
        [Import(typeof(VisualStudioWorkspace))]
        Workspace workspace,
        IWorkspaceChangeIndicator workspaceChangeIndicator,
        IAnalysisRequester analysisRequester,
        ILogger logger,
        IThreadHandling threadHandling)
    {
        this.workspace = workspace;
        this.analysisRequester = analysisRequester;
        this.threadHandling = threadHandling;
        this.workspaceChangeIndicator = workspaceChangeIndicator;
        workspace.WorkspaceChanged += WorkspaceOnWorkspaceChanged;
        quickFixApplicationLogger = logger.ForContext(Resources.RoslynLogContext, Resources.RoslynQuickFixLogContext);
    }

    public IRoslynSolutionWrapper GetCurrentSolution() => new RoslynSolutionWrapper(workspace.CurrentSolution);

    public Task<bool> ApplyOrMergeChangesAsync(IRoslynSolutionWrapper originalSolution, Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation operation, CancellationToken cancellationToken) =>
        ApplyChangesOperation.ApplyOrMergeChangesAsync(workspace, originalSolution.RoslynSolution, operation.ChangedSolution, quickFixApplicationLogger, workspaceChangeIndicator, cancellationToken);

    // todo SLVS-2466 add roslyn 'integration' tests using AdHocWorkspace
    // ideally, if doing a refactoring, this code should be moved closer to FileStateManager/LinkedFileAnalyzer by exposing the original event via IRoslynWorkspaceWrapper instead
    private void WorkspaceOnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
    {
        if (workspaceChangeIndicator.IsChangeKindTrivial(e.Kind))
        {
            return;
        }

        threadHandling.RunOnBackgroundThread(() =>
        {
            var solutionChanges = e.NewSolution.GetChanges(e.OldSolution);

            if (workspaceChangeIndicator.SolutionChangedCritically(solutionChanges)
                || solutionChanges.GetProjectChanges().Any(changedProject => workspaceChangeIndicator.ProjectChangedCritically(changedProject)))
            {
                analysisRequester.QueueAnalyzeOpenFiles();
            }
        }).Forget();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        workspace.WorkspaceChanged -= WorkspaceOnWorkspaceChanged;
        disposed = true;
    }
}
