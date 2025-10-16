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

using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;

internal class LiveAnalysisState(ITaskExecutorWithDebounce executor, IIssueTracker file, IFileTracker fileTracker) : IDisposable
{
    private bool disposed;
    private readonly object locker = new();

    public bool IsWaiting
    {
        get
        {
            lock (locker)
            {
                return !disposed && !executor.IsScheduled;
            }
        }
    }

    public void HandleLiveAnalysisEvent(Func<Task> postAnalysisAction)
    {
        lock (locker)
        {
            if (disposed)
            {
                return;
            }

            executor.Debounce(() =>
            {
                AnalyzeFile();
                postAnalysisAction?.Invoke().Forget(); // todo schedule for debounce??
            });
        }
    }

    private void AnalyzeFile()
    {
        var analysisSnapshot = file.UpdateAnalysisState();
        fileTracker.AddFiles(new SourceFile(analysisSnapshot.FilePath, content: analysisSnapshot.TextSnapshot.GetText()));
    }

    public void HandleBackgroundAnalysisEvent()
    {
        lock (locker)
        {
            if (!IsWaiting)
            {
                return;
            }

            executor.Debounce(AnalyzeFile, TimeSpan.FromSeconds(2));
        }
    }

    public void Dispose()
    {
        lock (locker)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            executor.Dispose();
        }
    }
}
