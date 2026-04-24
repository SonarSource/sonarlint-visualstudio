/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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
using Microsoft.VisualStudio.Text;
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

    bool TryGetCurrentSnapshot(string filePath, out ITextSnapshot snapshot);
}

internal interface ILinkedFileStateManager
{
    event EventHandler<LinkedAnalysisRequiredEventArgs> LinkedAnalysisRequested;

    IEnumerable<ILiveAnalysisState> GetAllStates();
}

[Export(typeof(ILinkedFileStateManager))]
[Export(typeof(IFileStateManager))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class FileStateManager(ILiveAnalysisStateFactory liveAnalysisStateFactory) : IFileStateManager, ILinkedFileStateManager
{
    private readonly object locker = new();
    private ImmutableDictionary<IFileState, ILiveAnalysisState> states = ImmutableDictionary<IFileState, ILiveAnalysisState>.Empty;

    public event EventHandler<LinkedAnalysisRequiredEventArgs> LinkedAnalysisRequested;

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
        state.LinkedAnalysisRequested += OnStateLinkedAnalysisRequested;
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
            state.LinkedAnalysisRequested -= OnStateLinkedAnalysisRequested;
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

    private void OnStateLinkedAnalysisRequested(object sender, LinkedAnalysisRequiredEventArgs e) =>
        LinkedAnalysisRequested?.Invoke(this, e);

    public bool TryGetCurrentSnapshot(string filePath, out ITextSnapshot snapshot)
    {
        lock (locker)
        {
            var fileState = states.Keys.FirstOrDefault(fs => fs.FilePath == filePath);
            if (fileState != null)
            {
                var fileSnapshot = fileState.UpdateFileState();
                snapshot = fileSnapshot?.TextSnapshot;
                return snapshot != null;
            }
            snapshot = null;
            return false;
        }
    }
}
