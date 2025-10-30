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
using Document = SonarLint.VisualStudio.Core.Document;

namespace SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;

internal interface IFileStateManager
{
    Document[] GetOpenDocuments();

    void AnalyzeAllOpenFiles();

    void Opened(IFileState file);

    void Closed(IFileState file);

    void Renamed(IFileState file);

    void ContentSaved(IFileState file);

    void ContentChanged(IFileState file);
}

internal interface IAnalysisStateProvider
{
    IEnumerable<ILiveAnalysisState> GetAllStates();
}

[Export(typeof(IAnalysisStateProvider))]
[Export(typeof(IFileStateManager))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class FileStateManager(ILiveAnalysisStateFactory liveAnalysisStateFactory) : IFileStateManager, IAnalysisStateProvider
{
    private readonly object locker = new();
    private ImmutableDictionary<IFileState, ILiveAnalysisState> states = ImmutableDictionary<IFileState, ILiveAnalysisState>.Empty;

    public Document[] GetOpenDocuments()
    {
        lock (locker)
        {
            return states.Keys.Select(it => new Document(it.FilePath, it.DetectedLanguages)).ToArray();
        }
    }

    public IEnumerable<ILiveAnalysisState> GetAllStates()
    {
        lock (locker)
        {
            return states.Values;
        }
    }

    public void AnalyzeAllOpenFiles()
    {
        lock (locker)
        {
            foreach (var issueTracker in states)
            {
                issueTracker.Value.HandleBackgroundAnalysisEvent();
            }
        }
    }

    public void Opened(IFileState file) =>
        HandleFileUpdate(file, false);

    private ILiveAnalysisState CreateAnalysisState(IFileState file)
    {

        var state = liveAnalysisStateFactory.Create(file);
        states = states.Add(file, state);
        return state;
    }

    public void Closed(IFileState file)
    {
        lock (locker)
        {
            if (!states.TryGetValue(file, out var state))
            {
                return;
            }
            states = states.Remove(file);
            state.Dispose();
        }
    }

    public void Renamed(IFileState file) => HandleFileUpdate(file);

    public void ContentSaved(IFileState file) => HandleFileUpdate(file);

    public void ContentChanged(IFileState file) => HandleFileUpdate(file);

    private void HandleFileUpdate(IFileState file, bool performLinkedAnalysis = true)
    {
        lock (locker)
        {
            if (!states.TryGetValue(file, out var state))
            {
                state = CreateAnalysisState(file);
            }
            state.HandleLiveAnalysisEvent(performLinkedAnalysis);
        }
    }
}
