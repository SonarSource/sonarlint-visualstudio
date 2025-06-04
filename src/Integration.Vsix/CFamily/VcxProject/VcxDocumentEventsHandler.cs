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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.CFamily;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

public interface IVcxDocumentEventsHandler : IDisposable
{
}

[Export(typeof(IVcxDocumentEventsHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class VcxDocumentEventsHandler : IVcxDocumentEventsHandler
{
    private readonly IDocumentEvents documentEvents;
    private readonly IVcxCompilationDatabaseUpdater vcxCompilationDatabaseUpdater;
    private bool disposed;

    [ImportingConstructor]
    public VcxDocumentEventsHandler(
        IDocumentEvents documentEvents,
        IVcxCompilationDatabaseUpdater vcxCompilationDatabaseUpdater)
    {
        this.documentEvents = documentEvents;
        this.vcxCompilationDatabaseUpdater = vcxCompilationDatabaseUpdater;
        this.documentEvents.DocumentOpened += DocumentEventsOnDocumentOpened;
        this.documentEvents.DocumentClosed += DocumentEventsOnDocumentClosed;
        this.documentEvents.OpenDocumentRenamed += DocumentEventsOnOpenDocumentRenamed;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        documentEvents.DocumentOpened -= DocumentEventsOnDocumentOpened;
        documentEvents.DocumentClosed -= DocumentEventsOnDocumentClosed;
        documentEvents.OpenDocumentRenamed -= DocumentEventsOnOpenDocumentRenamed;
    }

    private void DocumentEventsOnOpenDocumentRenamed(object sender, DocumentRenamedEventArgs e)
    {
        if (e.DetectedLanguages.Contains(AnalysisLanguage.CFamily))
        {
            ReplaceFileAsync(e.OldFilePath, e.FullPath).Forget();
        }
        return;

        async Task ReplaceFileAsync(string oldFilePath, string fullPath)
        {
            await vcxCompilationDatabaseUpdater.RemoveFileAsync(oldFilePath);
            await vcxCompilationDatabaseUpdater.AddFileAsync(fullPath);
        }
    }

    private void DocumentEventsOnDocumentClosed(object sender, DocumentClosedEventArgs e)
    {
        if (e.DetectedLanguages.Contains(AnalysisLanguage.CFamily))
        {
            vcxCompilationDatabaseUpdater.RemoveFileAsync(e.FullPath).Forget();
        }
    }

    private void DocumentEventsOnDocumentOpened(object sender, DocumentOpenedEventArgs e)
    {
        if (e.DetectedLanguages.Contains(AnalysisLanguage.CFamily))
        {
            vcxCompilationDatabaseUpdater.AddFileAsync(e.FullPath).Forget();
        }
    }
}
