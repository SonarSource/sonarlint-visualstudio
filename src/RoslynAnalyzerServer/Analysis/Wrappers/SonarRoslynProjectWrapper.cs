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

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

[ExcludeFromCodeCoverage] // todo SLVS-2466 add roslyn 'integration' tests using AdHocWorkspace
internal class SonarRoslynProjectWrapper(Project project) : ISonarRoslynProjectWrapper
{
    public string Name => project.Name;
    public bool SupportsCompilation => project.SupportsCompilation;
    public AnalyzerOptions RoslynAnalyzerOptions  => project.AnalyzerOptions;

    public async Task<ISonarRoslynCompilationWrapper> GetCompilationAsync(CancellationToken token) =>
        new SonarRoslynCompilationWrapper((await project.GetCompilationAsync(token))!);

    public bool ContainsDocument(
        string filePath,
        [NotNullWhen(true)]out string? analysisFilePath)
    {
        analysisFilePath = project.Documents
            .Select(document => document.FilePath)
            .Where(path => path != null)
            .FirstOrDefault(path =>
                path!.Equals(filePath)
                || (path.StartsWith(filePath)
                    && path.EndsWith(".g.cs")));

        return analysisFilePath != null;
    }

}
