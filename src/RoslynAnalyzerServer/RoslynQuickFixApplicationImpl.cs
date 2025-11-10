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

using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer;

public class RoslynQuickFixApplicationImpl
{
    private readonly IRoslynWorkspaceWrapper workspace;
    private readonly IRoslynSolutionWrapper originalSolution;
    internal readonly IRoslynCodeActionWrapper RoslynCodeAction;

    internal RoslynQuickFixApplicationImpl(
        IRoslynWorkspaceWrapper workspace,
        IRoslynSolutionWrapper originalSolution,
        IRoslynCodeActionWrapper roslynCodeAction,
        string filePath)
    {
        this.workspace = workspace;
        this.originalSolution = originalSolution;
        RoslynCodeAction = roslynCodeAction;
        FilePath = filePath;
    }

    public Guid Id { get; }  = Guid.NewGuid();
    public string FilePath { get; }
    public string Message => RoslynCodeAction.Title;

    public async Task<bool> ApplyAsync(CancellationToken cancellationToken)
    {
        var codeActionOperations = await RoslynCodeAction.GetOperationsAsync(cancellationToken);

        var applyChangesOperation = codeActionOperations.FirstOrDefault(x => x is Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation) as Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation;

        // we're only interested in ApplyChangesOperation and there can only be one at a time: https://github.com/dotnet/roslyn/blob/75e79dace86b274327a1afe479228d82a06051a4/src/Workspaces/Core/Portable/CodeActions/Operations/ApplyChangesOperation.cs#L18
        if (applyChangesOperation == null || codeActionOperations.Length > 1)
        {
            Debug.Fail($"Unexpected quickfix result: {applyChangesOperation} out of {codeActionOperations.Length}");
            return false;
        }

        return await workspace.ApplyOrMergeChangesAsync(originalSolution, applyChangesOperation, cancellationToken);
    }
}
