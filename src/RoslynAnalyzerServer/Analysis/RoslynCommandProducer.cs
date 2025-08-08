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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

internal interface ISonarRoslynSolutionAnalysisCommandProvider
{
    List<SonarRoslynProjectAnalysisCommands> GetAnalysisCommandsForCurrentSolution(string[] filePaths);
}

[Export(typeof(ISonarRoslynSolutionAnalysisCommandProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class SonarRoslynSolutionAnalysisCommandProvider(
    ISonarRoslynWorkspaceWrapper roslynWorkspaceWrapper,
    ILogger logger) : ISonarRoslynSolutionAnalysisCommandProvider
{
    public List<SonarRoslynProjectAnalysisCommands> GetAnalysisCommandsForCurrentSolution(string[] filePaths)
    {
        var result = new List<SonarRoslynProjectAnalysisCommands>();

        var solution = roslynWorkspaceWrapper.CurrentSolution;

        // todo this will not find unloaded projects
        foreach (var project in solution.Projects)
        {
            var commands = new List<ISonarRoslynAnalysisCommand>();

            foreach (string filePath in filePaths)
            {
                if (project.ContainsDocument(filePath, out var analysisFilePath))
                {
                    commands.Add(new SonarRoslynFileSyntaxAnalysis(analysisFilePath));
                    commands.Add(new SonarRoslynFileSemanticAnalysis(analysisFilePath));
                }
            }

            if (commands.Any())
            {
                result.Add(new SonarRoslynProjectAnalysisCommands(project, commands));
            }
            else
            {
                logger.LogVerbose("No files to analyze in project {0}", project.Name);
            }
        }

        if (!result.Any())
        {
            logger.WriteLine("No projects to analyze");
        }

        return result;
    }
}
