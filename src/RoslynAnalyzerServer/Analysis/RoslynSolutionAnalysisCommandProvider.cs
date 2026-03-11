/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

[Export(typeof(IRoslynSolutionAnalysisCommandProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class RoslynSolutionAnalysisCommandProvider(
    IRoslynWorkspaceWrapper roslynWorkspaceWrapper,
    ILogger logger) : IRoslynSolutionAnalysisCommandProvider
{
    private readonly ILogger logger = logger.ForContext(Resources.RoslynLogContext, Resources.RoslynAnalysisLogContext, Resources.RoslynAnalysisConfigurationLogContext);

    public List<RoslynProjectAnalysisRequest> GetAnalysisCommandsForCurrentSolution(string[] filePaths)
    {
        var result = new List<RoslynProjectAnalysisRequest>();

        var solution = roslynWorkspaceWrapper.GetCurrentSolution();

        foreach (var project in solution.Projects)
        {
            if (!project.SupportsCompilation)
            {
                logger.LogVerbose(Resources.AnalysisCommandProvider_NoCompilationForProject, project.Name);
                continue;
            }

            var targetFilePaths = GetTargetFilePathsForProject(filePaths, project).ToImmutableHashSet();

            if (targetFilePaths.Count > 0)
            {
                var scope = new ProjectAnalysisRequestScope(project, targetFilePaths);
                var command = new RoslynProjectAnalysisCommand(targetFilePaths);
                result.Add(new RoslynProjectAnalysisRequest(scope, [command], targetFilePaths.Select(x => new RoslynFileSyntaxAnalysisCommand(x, logger)).ToList()));
            }
        }

        if (!result.Any())
        {
            logger.WriteLine(Resources.AnalysisCommandProvider_NoProjects);
        }

        return result;
    }

    private static IEnumerable<string> GetTargetFilePathsForProject(string[] filePaths, IRoslynProjectWrapper project)
    {
        foreach (var filePath in filePaths)
        {
            if (project.ContainsDocument(filePath, out var document))
            {
                yield return document.FilePath!;
            }
        }
    }
}
