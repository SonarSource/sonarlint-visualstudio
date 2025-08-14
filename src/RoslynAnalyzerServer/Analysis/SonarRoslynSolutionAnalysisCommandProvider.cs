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

[Export(typeof(ISonarRoslynSolutionAnalysisCommandProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class SonarRoslynSolutionAnalysisCommandProvider(
    ISonarRoslynWorkspaceWrapper roslynWorkspaceWrapper,
    ILogger logger) : ISonarRoslynSolutionAnalysisCommandProvider
{
    private readonly ILogger logger = logger.ForContext("Roslyn Analysis", "Configuration");

    public List<SonarRoslynProjectAnalysisRequest> GetAnalysisCommandsForCurrentSolution(string[] filePaths)
    {
        var result = new List<SonarRoslynProjectAnalysisRequest>();

        var solution = roslynWorkspaceWrapper.GetCurrentSolution();

        foreach (var project in solution.Projects)
        {
            if (!project.SupportsCompilation)
            {
                logger.LogVerbose("Project {0} does not support compilation", project.Name);
                continue;
            }

            var commands = GetCompilationCommandsForProject(filePaths, project);

            if (commands.Any())
            {
                result.Add(new SonarRoslynProjectAnalysisRequest(project, commands));
            }
        }

        if (!result.Any())
        {
            logger.WriteLine("No projects to analyze");
        }

        return result;
    }

    private List<ISonarRoslynAnalysisCommand> GetCompilationCommandsForProject(string[] filePaths, ISonarRoslynProjectWrapper project)
    {
        var commands = new List<ISonarRoslynAnalysisCommand>();

        foreach (var filePath in filePaths)
        {
            if (!project.ContainsDocument(filePath, out var analysisFilePath))
            {
                continue;
            }

            commands.Add(new SonarRoslynFileSyntaxAnalysis(analysisFilePath, logger));
            commands.Add(new SonarRoslynFileSemanticAnalysis(analysisFilePath, logger));
        }
        return commands;
    }
}
