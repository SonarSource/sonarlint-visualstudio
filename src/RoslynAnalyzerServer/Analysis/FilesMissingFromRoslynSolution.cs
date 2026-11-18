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
    /// <summary>
    /// Raised after <see cref="Add"/> records new files. Their project may have loaded while they were being
    /// analyzed, so listeners should check them against the current solution.
    /// </summary>
    event EventHandler? FilesAdded;

    bool HasAny { get; }

    void Add(IEnumerable<string> filePaths);

    /// <summary>Forgets the files that <paramref name="solution"/> now contains, and returns whether there were any.</summary>
    bool RemoveFilesContainedIn(IRoslynSolutionWrapper solution);

    void Clear();
}

[Export(typeof(IFilesMissingFromRoslynSolution))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class FilesMissingFromRoslynSolution : IFilesMissingFromRoslynSolution
{
    private readonly ConcurrentDictionary<string, byte> filePaths = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler? FilesAdded;

    public bool HasAny => !filePaths.IsEmpty;

    public void Add(IEnumerable<string> filePaths)
    {
        var addedAny = false;
        foreach (var filePath in filePaths)
        {
            addedAny |= this.filePaths.TryAdd(filePath, 0);
        }
        if (addedAny)
        {
            FilesAdded?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool RemoveFilesContainedIn(IRoslynSolutionWrapper solution)
    {
        var removedAny = false;
        foreach (var filePath in filePaths.Keys)
        {
            // The same condition as the analysis uses to pick the file's projects.
            if (solution.Projects.Any(project => project.SupportsCompilation && project.ContainsDocument(filePath, out _))
                && filePaths.TryRemove(filePath, out _))
            {
                removedAny = true;
            }
        }
        return removedAny;
    }

    public void Clear() => filePaths.Clear();
}
