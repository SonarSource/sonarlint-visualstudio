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
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

[ExcludeFromCodeCoverage] // todo SLVS-2466 add roslyn 'integration' tests using AdHocWorkspace
public class RoslynCompilationWithAnalyzersWrapper(CompilationWithAnalyzers compilation, Language language) : IRoslynCompilationWithAnalyzersWrapper
{
    public Language Language { get; } = language;

    public SyntaxTree? GetSyntaxTree(string filePath) => compilation.Compilation.SyntaxTrees.SingleOrDefault(x => filePath.Equals(x.FilePath));

    public SemanticModel? GetSemanticModel(string filePath) => GetSyntaxTree(filePath) is {} syntaxTree ? compilation.Compilation.GetSemanticModel(syntaxTree) : null;

    public Task<ImmutableArray<Diagnostic>> GetAnalyzerSyntaxDiagnosticsAsync(SyntaxTree syntaxTree, CancellationToken token) =>
        compilation.GetAnalyzerSyntaxDiagnosticsAsync(syntaxTree, token);

    public Task<ImmutableArray<Diagnostic>> GetAnalyzerSemanticDiagnosticsAsync(SemanticModel semanticModel, CancellationToken token) =>
        compilation.GetAnalyzerSemanticDiagnosticsAsync(semanticModel, null, token);
}
