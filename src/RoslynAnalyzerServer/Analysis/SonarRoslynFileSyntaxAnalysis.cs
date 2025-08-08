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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

internal class SonarRoslynFileSyntaxAnalysis(string analysisFilePath) : ISonarRoslynAnalysisCommand
{
    public async Task<IEnumerable<Diagnostic>> ExecuteAsync(ISonarRoslynCompilationWithAnalyzersWrapper compilation, CancellationToken token)
    {
        var roslynCompilation = compilation.RoslynCompilation;

        var syntaxTree = roslynCompilation.Compilation.SyntaxTrees.SingleOrDefault(x => analysisFilePath.Equals(x.FilePath));
        if (syntaxTree == null)
        {
            return []; // todo log?
        }

        return await roslynCompilation.GetAnalyzerSyntaxDiagnosticsAsync(syntaxTree, token);
    }
}
