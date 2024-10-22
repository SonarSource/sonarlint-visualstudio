/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

internal interface IRoslynSolutionWrapper
{
    IRoslynSolutionWrapper RemoveAnalyzerReferences(ImmutableArray<AnalyzerFileReference> analyzers);
    IRoslynSolutionWrapper WithAnalyzerReferences(ImmutableArray<AnalyzerFileReference> analyzers);
    Solution GetRoslynSolution();
}

internal class RoslynSolutionWrapper(Solution solution) : IRoslynSolutionWrapper
{
    public IRoslynSolutionWrapper RemoveAnalyzerReferences(ImmutableArray<AnalyzerFileReference> analyzers) =>
        new RoslynSolutionWrapper(
            analyzers
                .Aggregate<AnalyzerFileReference, Solution>(
                    solution,
                    (current, analyzer) =>
                        current.AnalyzerReferences.Contains(analyzer)
                            ? current.RemoveAnalyzerReference(analyzer)
                            : current));

    public IRoslynSolutionWrapper WithAnalyzerReferences(ImmutableArray<AnalyzerFileReference> analyzers) => 
        new RoslynSolutionWrapper(solution.WithAnalyzerReferences(analyzers));

    public Solution GetRoslynSolution() => solution;
}
