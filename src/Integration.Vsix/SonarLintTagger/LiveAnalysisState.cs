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
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;

internal interface ILiveAnalysisStateFactory
{
    ILiveAnalysisState Create(IFileState fileState);
}

[Export(typeof(ILiveAnalysisStateFactory))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class LiveAnalysisStateFactory(
    ITaskExecutorWithDebounceFactory taskExecutorWithDebounceFactory,
    IFileTracker fileTracker,
    ILinkedFileAnalyzer linkedFileAnalyzer) : ILiveAnalysisStateFactory
{
    public ILiveAnalysisState Create(IFileState fileState) =>
        new LiveAnalysisState(taskExecutorWithDebounceFactory.Create(), fileState, fileTracker, linkedFileAnalyzer);
}

internal interface ILiveAnalysisState : IDisposable
{
    IFileState FileState { get; }
    bool IsWaiting { get; }

    void HandleLiveAnalysisEvent(bool triggerLinkedAnalysis);

    void HandleBackgroundAnalysisEvent();
}

internal sealed class LiveAnalysisState(
    ITaskExecutorWithDebounce executor,
    IFileState file,
    IFileTracker fileTracker,
    ILinkedFileAnalyzer linkedFileAnalyzer)
    : ILiveAnalysisState
{
    internal static readonly TimeSpan LiveAnalysisDebounceDuration = TimeSpan.FromMilliseconds(700);
    internal static readonly TimeSpan LinkCalculationDebounceDuration = TimeSpan.FromSeconds(1);
    internal static readonly TimeSpan BackgroundAnalysisDebounceDuration = TimeSpan.FromSeconds(2);

    private bool disposed;

    public IFileState FileState => file;

    public bool IsWaiting => !disposed && !executor.IsScheduled;

    public void HandleLiveAnalysisEvent(bool triggerLinkedAnalysis)
    {
        if (disposed)
        {
            return;
        }

        executor.Debounce(
            _ =>
            {
                AnalyzeFile();
                if (triggerLinkedAnalysis)
                {
                    executor.Debounce(token => linkedFileAnalyzer.ScheduleLinkedAnalysis(file, token), LinkCalculationDebounceDuration);
                }
            },
            LiveAnalysisDebounceDuration);
    }

    private void AnalyzeFile()
    {
        var analysisSnapshot = file.UpdateFileState();
        fileTracker.AddFiles(new SourceFile(analysisSnapshot.FilePath, content: analysisSnapshot.TextSnapshot.GetText()));
    }

    public void HandleBackgroundAnalysisEvent()
    {
        if (!IsWaiting)
        {
            return;
        }

        executor.Debounce(_ => AnalyzeFile(), BackgroundAnalysisDebounceDuration);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        executor.Dispose();
    }
}
