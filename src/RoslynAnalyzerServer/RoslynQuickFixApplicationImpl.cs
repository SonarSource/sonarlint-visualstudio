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

using Microsoft.CodeAnalysis.CodeActions;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer;

public class RoslynQuickFixApplicationImpl
{
    private readonly IRoslynWorkspaceWrapper workspace;
    private readonly IRoslynSolutionWrapper originalSolution;
    private readonly CodeAction codeAction;

    internal RoslynQuickFixApplicationImpl(IRoslynWorkspaceWrapper workspace, IRoslynSolutionWrapper originalSolution, CodeAction codeAction)
    {
        this.workspace = workspace;
        this.originalSolution = originalSolution;
        this.codeAction = codeAction;
    }

    public string Message => codeAction.Title;

    public async Task<bool> ApplyAsync(CancellationToken cancellationToken)
    {
        var codeActionOperations = await codeAction.GetOperationsAsync(cancellationToken);

        var applyChangesOperation = codeActionOperations.FirstOrDefault(x => x is ApplyChangesOperation) as ApplyChangesOperation;

        if (applyChangesOperation == null)
        {
            return false;
        }

        if (codeActionOperations.Length > 1)
        {
            // todo ???
        }

        return await workspace.ApplyOrMergeChangesAsync(originalSolution, applyChangesOperation, cancellationToken);
    }
}
