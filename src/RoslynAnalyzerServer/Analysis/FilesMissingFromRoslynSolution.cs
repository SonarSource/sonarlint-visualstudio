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

using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

/// <summary>
/// Files that an analysis could not find in any Roslyn project, typically because their project was still loading.
/// Such an analysis reports no issues, so the files are analyzed again once Roslyn's solution contains them.
/// </summary>
internal interface IFilesMissingFromRoslynSolution
{
    bool HasAny { get; }

    void Add(IEnumerable<string> filePaths);

    /// <summary>Forgets the files that <paramref name="solution"/> now contains, and returns whether there were any.</summary>
    /// <remarks>Each file is retried at most a few times per solution, so a file that keeps being missed can't loop.</remarks>
    bool RemoveFilesContainedIn(IRoslynSolutionWrapper solution);

    void Clear();
}

[Export(typeof(IFilesMissingFromRoslynSolution))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class FilesMissingFromRoslynSolution : IFilesMissingFromRoslynSolution
{
    internal const int MaxRetriesPerFile = 3;
    private readonly ConcurrentDictionary<string, byte> filePaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> retries = new(StringComparer.OrdinalIgnoreCase);

    public bool HasAny => !filePaths.IsEmpty;

    public void Add(IEnumerable<string> filePaths)
    {
        foreach (var filePath in filePaths)
        {
            if (!retries.TryGetValue(filePath, out var count) || count < MaxRetriesPerFile)
            {
                this.filePaths.TryAdd(filePath, 0);
            }
        }
    }

    public bool RemoveFilesContainedIn(IRoslynSolutionWrapper solution)
    {
        var removedAny = false;
        foreach (var filePath in filePaths.Keys)
        {
            if (solution.ContainsCompilableDocument(filePath) && filePaths.TryRemove(filePath, out _))
            {
                retries.AddOrUpdate(filePath, 1, (_, count) => count + 1);
                removedAny = true;
            }
        }
        return removedAny;
    }

    public void Clear()
    {
        filePaths.Clear();
        retries.Clear();
    }
}
