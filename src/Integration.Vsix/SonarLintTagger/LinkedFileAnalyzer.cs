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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;

internal interface ILinkedFileAnalyzer
{
    void ScheduleLinkedAnalysis(IFileState file, CancellationToken token);
}

[Export(typeof(ILinkedFileAnalyzer))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method:ImportingConstructor]
internal class LinkedFileAnalyzer(
    ITypeReferenceFinder typeReferenceFinder,
    Lazy<IAnalysisStateProvider> analysisStateProvider,
    IRoslynWorkspaceWrapper workspaceWrapper,
    ILogger logger)
    : ILinkedFileAnalyzer
{
    private readonly ILogger logger = logger.ForVerboseContext(nameof(LinkedFileAnalyzer));

    public void ScheduleLinkedAnalysis(IFileState file, CancellationToken token) => RunLinkedAnalysisAsync(file, token).Forget();

    private async Task RunLinkedAnalysisAsync(IFileState file, CancellationToken token)
    {
        try
        {
            logger.LogVerbose("Starting affected files calculation for {0}", file.FilePath);
            await RunLinkedAnalysisInternalAsync(file, token);
        }
        catch (Exception e)
        {
            logger.LogVerbose(e.ToString());
        }
    }

    private async Task RunLinkedAnalysisInternalAsync(IFileState file, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();

        var solution = workspaceWrapper.GetCurrentSolution();
        if (!solution.ContainsDocument(file.FilePath, out var sourceDocument))
        {
            return;
        }

        var states = analysisStateProvider.Value.GetAllStates();
        var otherDocuments = GetRoslynDocuments(
            states,
            file,
            solution);

        if (otherDocuments.Count == 0)
        {
            return;
        }

        var linkedDocuments = await typeReferenceFinder.GetCrossFileReferencesInScopeAsync(
            sourceDocument,
            otherDocuments.Keys,
            solution,
            token);

        foreach (var fileModel in linkedDocuments)
        {
            otherDocuments[fileModel].HandleLiveAnalysisEvent(false);
        }

        logger.LogVerbose("Linked file calculation took {0} ms, found {1} for {2}", stopwatch.Elapsed.TotalMilliseconds, linkedDocuments.Count, file.FilePath);
    }

    private static Dictionary<IRoslynDocumentWrapper, ILiveAnalysisState> GetRoslynDocuments(
        IEnumerable<ILiveAnalysisState> states,
        IFileState excludeFile,
        IRoslynSolutionWrapper solution)
    {
        Dictionary<IRoslynDocumentWrapper, ILiveAnalysisState> fileModels = new();
        foreach (var state in states)
        {
            var tracker = state.FileState;
            if (tracker == excludeFile || !state.IsWaiting)
            {
                continue;
            }
            if (!solution.ContainsDocument(tracker.FilePath, out var otherDocument))
            {
                continue;
            }
            fileModels.Add(otherDocument, state);
        }
        return fileModels;
    }
}
