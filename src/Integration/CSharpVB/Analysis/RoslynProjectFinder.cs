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
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;
using Document = Microsoft.CodeAnalysis.Document;

namespace SonarLint.VisualStudio.Integration.CSharpVB.Analysis;

internal interface IRoslynDocumentFinder
{
    List<(Project project, string analysisFilePath)> FindProjectsWithDocument(string filePath);
}

[Export(typeof(IRoslynDocumentFinder))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class RoslynProjectFinder(
    IRoslynWorkspaceWrapper roslynWorkspaceWrapper,
    ILogger logger) : IRoslynDocumentFinder
{
    public List<(Project project, string analysisFilePath)> FindProjectsWithDocument(string filePath)
    {
        var solution = roslynWorkspaceWrapper.CurrentSolution.RoslynSolution;

        var result = new List<(Project, string)>();

        foreach (var roslynSolutionProject in solution.Projects)
        {
            foreach (var document in roslynSolutionProject.Documents)
            {
                if (CompareFilePath(filePath, document, out var analysisFilePath))
                {
                    result.Add((roslynSolutionProject, analysisFilePath));
                }
            }
        }

        if (result.Count == 0)
        {
            logger.WriteLine($"No projects found containing file: {filePath}");
        }

        return result;
    }

    private static bool CompareFilePath(
        string filePath,
        Document document,
        out string analysisFilePath)
    {
        analysisFilePath = null;
        if (document.FilePath is null)
        {
            return false;
        }

        if (!document.FilePath.Equals(filePath)
            && (!document.FilePath.StartsWith(filePath) || !document.FilePath.EndsWith(".g.cs")))
        {
            return false;
        }

        analysisFilePath = document.FilePath;
        return true;
    }
}
