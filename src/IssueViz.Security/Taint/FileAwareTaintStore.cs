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
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint;

[Export(typeof(IFileAwareTaintStore))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class FileAwareTaintStore : IFileAwareTaintStore
{
    private readonly ITaintStore taintStore;
    private readonly IDocumentTracker documentTracker;
    private bool disposed;

    [ImportingConstructor]
    public FileAwareTaintStore(ITaintStore taintStore, IDocumentTracker documentTracker)
    {
        this.taintStore = taintStore;
        this.documentTracker = documentTracker;
        taintStore.IssuesChanged += TaintStore_IssuesChanged;
        documentTracker.DocumentClosed += DocumentTracker_DocumentClosed;
        documentTracker.DocumentOpened += DocumentTracker_DocumentOpened;
        documentTracker.OpenDocumentRenamed += DocumentTracker_OpenDocumentRenamed;
    }

    private void TaintStore_IssuesChanged(object sender, IssuesChangedEventArgs e) =>
        NotifyIssuesChanged(e.RemovedIssues, GetTaintsFromOpenFiles(e.AddedIssues).ToList());

    private void DocumentTracker_OpenDocumentRenamed(object sender, DocumentRenamedEventArgs e) => HandleTaintFileClosed(e.OldFilePath);

    private void DocumentTracker_DocumentOpened(object sender, DocumentEventArgs e) => HandleTaintFileOpened(e.Document.FullPath);

    private void DocumentTracker_DocumentClosed(object sender, DocumentEventArgs e) => HandleTaintFileClosed(e.Document.FullPath);

    public IReadOnlyCollection<IAnalysisIssueVisualization> GetAll() => GetTaintsFromOpenFiles(taintStore.GetAll()).ToList();

    private IEnumerable<IAnalysisIssueVisualization> GetTaintsFromOpenFiles(IEnumerable<IAnalysisIssueVisualization> taints) =>
        documentTracker.GetOpenDocuments().Select(x => x.FullPath).ToHashSet() is { Count: > 0 } openFiles
            ? taints.Where(x => openFiles.Contains(x.CurrentFilePath))
            : [];

    private static IEnumerable<IAnalysisIssueVisualization> GetTaintsFromOpenFile(IEnumerable<IAnalysisIssueVisualization> taints, string filePath) =>
        taints.Where(x => x.CurrentFilePath == filePath);

    public event EventHandler<IssuesChangedEventArgs> IssuesChanged;

    public string ConfigurationScope => taintStore.ConfigurationScope;

    private void HandleTaintFileOpened(string filePath)
    {
        var analysisIssueVisualizations = GetTaintsFromOpenFile(taintStore.GetAll(), filePath).ToList();

        if (analysisIssueVisualizations.Any())
        {
            NotifyIssuesChanged([], analysisIssueVisualizations);
        }
    }

    private void HandleTaintFileClosed(string filePath)
    {
        var analysisIssueVisualizations = GetTaintsFromOpenFile(taintStore.GetAll(), filePath).ToList();

        if (analysisIssueVisualizations.Any())
        {
            NotifyIssuesChanged(analysisIssueVisualizations, []);
        }
    }

    private void NotifyIssuesChanged(
        IReadOnlyCollection<IAnalysisIssueVisualization> removedIssues,
        IReadOnlyCollection<IAnalysisIssueVisualization> addedIssues) =>
        IssuesChanged?.Invoke(this, new IssuesChangedEventArgs(removedIssues, addedIssues));

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        taintStore.IssuesChanged -= TaintStore_IssuesChanged;
        documentTracker.DocumentOpened -= DocumentTracker_DocumentClosed;
        documentTracker.DocumentClosed -= DocumentTracker_DocumentOpened;
        documentTracker.OpenDocumentRenamed -= DocumentTracker_OpenDocumentRenamed;
    }
}
