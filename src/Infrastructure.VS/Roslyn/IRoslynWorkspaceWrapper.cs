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

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.SystemAbstractions;

namespace SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

internal interface IAnalyzerChange
{
    ImmutableArray<AnalyzerFileReference> AnalyzersToAdd { get; }
    ImmutableArray<AnalyzerFileReference> AnalyzersToRemove { get; }

    IRoslynSolutionWrapper Change(IRoslynSolutionWrapper solution);
}

internal class AnalyzerChange(ImmutableArray<AnalyzerFileReference> analyzersToRemove, ImmutableArray<AnalyzerFileReference> analyzersToAdd) : IAnalyzerChange
{
    public ImmutableArray<AnalyzerFileReference> AnalyzersToAdd { get; } = analyzersToAdd;
    public ImmutableArray<AnalyzerFileReference> AnalyzersToRemove { get; } = analyzersToRemove;

    public IRoslynSolutionWrapper Change(IRoslynSolutionWrapper solution)
    {
        var analyzersToRemoveFiltered = AnalyzersToRemove.RemoveAll(x => !solution.ContainsAnalyzer(x));
        if (analyzersToRemoveFiltered.Any())
        {
            solution = solution.RemoveAnalyzerReferences(analyzersToRemoveFiltered);
        }
        var analyzersToAddFiltered = AnalyzersToAdd.RemoveAll(x => solution.ContainsAnalyzer(x));
        if (analyzersToAddFiltered.Any())
        {
            solution = solution.AddAnalyzerReferences(analyzersToAddFiltered);
        }
        return solution;
    }
}

internal interface IRoslynWorkspaceWrapper
{
    IRoslynSolutionWrapper CurrentSolution { get; }

    Task<IRoslynSolutionWrapper> TryApplyChangesAsync(IAnalyzerChange analyzerChange);
}

[Export(typeof(IRoslynWorkspaceWrapper))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class RoslynWorkspaceWrapper([Import(typeof(VisualStudioWorkspace))] Workspace workspace, IThreadHandling threadHandling) : IRoslynWorkspaceWrapper
{
    private const int ApplyRetryCount = 5;

    public IRoslynSolutionWrapper CurrentSolution =>
        new RoslynSolutionWrapper(workspace.CurrentSolution);

    public async Task<IRoslynSolutionWrapper> TryApplyChangesAsync(IAnalyzerChange analyzerChange)
    {
        for (var attempt = 0; attempt < ApplyRetryCount; attempt++)
        {
            if (await TryApplyChangesInternalAsync(analyzerChange) is {} result)
            {
                return result;
            }
        }

        return null;
    }

    private async Task<IRoslynSolutionWrapper> TryApplyChangesInternalAsync(IAnalyzerChange analyzerChange)
    {
        IRoslynSolutionWrapper wasApplied = null;
        await threadHandling.RunOnUIThreadAsync(() =>
        {
            var currentSolution = CurrentSolution;
            var updatedSolution = analyzerChange.Change(currentSolution);

            wasApplied = updatedSolution == currentSolution || workspace.TryApplyChanges(updatedSolution.RoslynSolution) ? CurrentSolution : null;
        });
        return wasApplied;
    }
}
