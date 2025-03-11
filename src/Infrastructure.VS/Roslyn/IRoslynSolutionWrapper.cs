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
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

internal interface IRoslynSolutionWrapper
{
    IRoslynSolutionWrapper RemoveAnalyzerReferences(IReadOnlyCollection<AnalyzerFileReference> analyzers);

    IRoslynSolutionWrapper AddAnalyzerReferences(IReadOnlyCollection<AnalyzerFileReference> analyzers);

    Solution RoslynSolution { get; }

    string DisplayCurrentAnalyzerState();

    bool ContainsAnalyzer(AnalyzerFileReference analyzer);
}

[ExcludeFromCodeCoverage]
internal class RoslynSolutionWrapper(Solution solution) : IRoslynSolutionWrapper
{
    public bool ContainsAnalyzer(AnalyzerFileReference analyzer) => solution.AnalyzerReferences.Contains(analyzer);

    public IRoslynSolutionWrapper RemoveAnalyzerReferences(IReadOnlyCollection<AnalyzerFileReference> analyzers) =>
        new RoslynSolutionWrapper(analyzers.Aggregate(solution, (current, analyzer) => current.RemoveAnalyzerReference(analyzer)));

    public IRoslynSolutionWrapper AddAnalyzerReferences(IReadOnlyCollection<AnalyzerFileReference> analyzers) =>
        new RoslynSolutionWrapper(solution.AddAnalyzerReferences(analyzers));

    public Solution RoslynSolution => solution;

    public string DisplayCurrentAnalyzerState()
    {
        try
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Solution {solution.FilePath}, {solution.Version} Analyzers");
            foreach (var currentSolutionAnalyzer in solution.AnalyzerReferences)
            {
                PrintAnalyzer(stringBuilder, currentSolutionAnalyzer);
            }

            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId)!;
                stringBuilder.AppendLine($"Project {project.Name} Analyzers");
                foreach (var projectAnalyzer in project.AnalyzerReferences)
                {
                    PrintAnalyzer(stringBuilder, projectAnalyzer);
                }
            }

            return stringBuilder.ToString();
        }
        catch
        {
            // this code is purely for debugging/investigating, so we don't care about exceptions here
            return "Failed to list solution analyzers";
        }
    }

    private static void PrintAnalyzer(StringBuilder stringBuilder, AnalyzerReference analyzer) => stringBuilder.AppendLine($"    {analyzer.DisplayInfo()}");
}

internal static class AnalyzerReferenceExtensions
{
    public static string DisplayInfo(this AnalyzerReference analyzer) => $"{analyzer?.Display}, {analyzer?.Id}, {analyzer?.FullPath}";
}
