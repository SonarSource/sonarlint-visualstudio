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

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

[ExcludeFromCodeCoverage] // todo SLVS-2466 add roslyn 'integration' tests using AdHocWorkspace
internal class RoslynSolutionWrapper : IRoslynSolutionWrapper
{
    public RoslynSolutionWrapper(Solution solution)
    {
        RoslynSolution = solution;
        Projects = solution.Projects.Select(x => new RoslynProjectWrapper(x, this));
    }

    public IEnumerable<IRoslynProjectWrapper> Projects { get; }
    public Solution RoslynSolution { get; }

    public IRoslynDocumentWrapper? GetDocument(SyntaxTree? tree) => RoslynSolution.GetDocument(tree) is { } roslynDocument ? new RoslynDocumentWrapper(roslynDocument) : null;

    public IRoslynDocumentWrapper? GetDocument(string filePath)
    {
        // todo search is not good

        var documentId = RoslynSolution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault();

        if (documentId is null || RoslynSolution.GetDocument(documentId) is not { } document)
        {
            return null;
        }

        return new RoslynDocumentWrapper(document);
    }
}
