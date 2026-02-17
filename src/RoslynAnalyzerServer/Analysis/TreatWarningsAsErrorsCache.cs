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
using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

/// <summary>
/// Internal interface for updating the TreatWarningsAsErrors cache
/// </summary>
internal interface ITreatWarningsAsErrorsCacheUpdater
{
    /// <summary>
    /// Updates the cache from all projects in the solution. If solution is null, clears the cache.
    /// </summary>
    void UpdateFromSolution(IRoslynSolutionWrapper solution);

    /// <summary>
    /// Updates the cache for a single project
    /// </summary>
    void UpdateForProject(string projectName, bool treatWarningsAsErrors);
}

[Export(typeof(ITreatWarningsAsErrorsCache))]
[Export(typeof(ITreatWarningsAsErrorsCacheUpdater))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class TreatWarningsAsErrorsCache : ITreatWarningsAsErrorsCache, ITreatWarningsAsErrorsCacheUpdater
{
    private readonly Dictionary<string, bool> cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object lockObject = new();

    public bool IsTreatWarningsAsErrorsEnabled(string projectName)
    {
        lock (lockObject)
        {
            return cache.TryGetValue(projectName, out var enabled) && enabled;
        }
    }

    public void UpdateFromSolution(IRoslynSolutionWrapper solution)
    {
        lock (lockObject)
        {
            cache.Clear();
            foreach (var project in solution.Projects)
            {
                var isTreatWarningsAsErrors = project.GeneralDiagnosticOption == ReportDiagnostic.Error;
                cache[project.Name] = isTreatWarningsAsErrors;
            }
        }
    }

    public void UpdateForProject(string projectName, bool treatWarningsAsErrors)
    {
        lock (lockObject)
        {
            cache[projectName] = treatWarningsAsErrors;
        }
    }
}
