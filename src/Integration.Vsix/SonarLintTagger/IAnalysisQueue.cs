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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;
using Document = SonarLint.VisualStudio.Core.Document;

namespace SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;

internal interface IAnalysisQueue
{
    Document[] GetOpenDocuments();

    void MultiFileReanalysis(IEnumerable<string> filePaths = null);

    void Opened(IIssueTracker file);

    void Closed(IIssueTracker file);

    void Renamed(IIssueTracker file);

    void ContentSaved(IIssueTracker file);

    void ContentChanged(IIssueTracker file);
}

[Export(typeof(IAnalysisQueue))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class AnalysisQueue(
    IFileTracker fileTracker,
    ITaskExecutorWithDebounceFactory taskExecutorFactory,
    IRoslynWorkspaceWrapper workspaceWrapper,
    ILogger logger)
    : IAnalysisQueue
{
    private readonly object lockObject = new();
    private ImmutableDictionary<IIssueTracker, LiveAnalysisState> states = ImmutableDictionary<IIssueTracker, LiveAnalysisState>.Empty;

    public Document[] GetOpenDocuments()
    {
        lock (lockObject)
        {
            return states.Keys.Select(it => new Document(it.FilePath, it.DetectedLanguages)).ToArray();
        }
    }

    public void MultiFileReanalysis(IEnumerable<string> filePaths)
    {
        lock (lockObject)
        {
            foreach (var issueTracker in FilterFiles(filePaths))
            {
                issueTracker.Value.HandleBackgroundAnalysisEvent();
            }
        }
    }

    private IEnumerable<KeyValuePair<IIssueTracker, LiveAnalysisState>> FilterFiles(IEnumerable<string> filePaths) => IssueTrackers(filePaths);

    private IEnumerable<KeyValuePair<IIssueTracker, LiveAnalysisState>> IssueTrackers(IEnumerable<string> filePaths)
    {
        if (filePaths == null || !filePaths.Any())
        {
            return states;
        }

        var paths = filePaths.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return states.Where(it => paths.Contains(it.Key.FilePath));
    }

    public void Opened(IIssueTracker file)
    {
        lock (lockObject)
        {
            var state = CreateAnalysisState(file);
            state.HandleLiveAnalysisEvent(null);
        }
    }

    private LiveAnalysisState CreateAnalysisState(IIssueTracker file)
    {
        var state = new LiveAnalysisState(taskExecutorFactory.Create(TimeSpan.FromMilliseconds(650)), file, fileTracker);
        states = states.Add(file, state); // todo duplicate ?
        return state;
    }

    public void Closed(IIssueTracker file)
    {
        lock (lockObject)
        {
            if (!states.TryGetValue(file, out var state))
            {
                return;
            }
            states = states.Remove(file);
            state.Dispose();
        }
    }

    public void Renamed(IIssueTracker file) => HandleFileUpdate(file);

    public void ContentSaved(IIssueTracker file) => HandleFileUpdate(file);

    public void ContentChanged(IIssueTracker file) => HandleFileUpdate(file);

    private void HandleFileUpdate(IIssueTracker file)
    {
        lock (lockObject)
        {
            if (!states.TryGetValue(file, out var state))
            {
                state = CreateAnalysisState(file);
            }
            state.HandleLiveAnalysisEvent(() => ScheduleLinkedFilesInBackgroundAsync(file));
        }
    }

    private async Task ScheduleLinkedFilesInBackgroundAsync(IIssueTracker file)
    {
        var stopwatch = Stopwatch.StartNew();
        IRoslynDocumentWrapper sourceFile;
        Dictionary<Microsoft.CodeAnalysis.Document, KeyValuePair<IIssueTracker, LiveAnalysisState>> fileModels;
        IRoslynSolutionWrapper roslynSolutionWrapper;
        lock (lockObject)
        {
            var newTasksToSchedule = FilterFiles(null).Where(x => x.Key != file && x.Value.IsWaiting);
            roslynSolutionWrapper = workspaceWrapper.GetCurrentSolution();
            sourceFile = roslynSolutionWrapper.GetDocument(file.FilePath)!;
            fileModels = newTasksToSchedule
                .Select(x => (State: x, RoslynDocumentWrapper: roslynSolutionWrapper.GetDocument(x.Key.FilePath)))
                .Where(x => x.RoslynDocumentWrapper != null)
                .ToDictionary(x => x.RoslynDocumentWrapper.RoslynDocument, x => x.State);
        }

        var filteredFileModels = await ScheduleLinkedFilesInBackgroundAsync(sourceFile, fileModels, roslynSolutionWrapper);

        logger.WriteLine("Linked file calculation took {0} ms, found {1} for {2}", stopwatch.Elapsed.TotalMilliseconds, filteredFileModels, file.FilePath);
    }

    private static async Task<int> ScheduleLinkedFilesInBackgroundAsync(
        IRoslynDocumentWrapper sourceFile,
        Dictionary<Microsoft.CodeAnalysis.Document, KeyValuePair<IIssueTracker, LiveAnalysisState>> fileModels,
        IRoslynSolutionWrapper roslynSolutionWrapper)
    {
        var filteredFileModels = await TypeReferenceFinder.GetCrossFileReferencesInScopeAsync(
            sourceFile.RoslynDocument,
            fileModels.Keys,
            roslynSolutionWrapper.RoslynSolution);

        foreach (var fileModel in filteredFileModels)
        {
            fileModels[fileModel].Value.HandleBackgroundAnalysisEvent();
        }
        return filteredFileModels.Count;
    }
}
