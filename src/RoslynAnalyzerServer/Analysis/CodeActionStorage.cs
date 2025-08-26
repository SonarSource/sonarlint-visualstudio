// /*
//  * SonarLint for Visual Studio
//  * Copyright (C) 2016-2025 SonarSource SA
//  * mailto:info AT sonarsource DOT com
//  *
//  * This program is free software; you can redistribute it and/or
//  * modify it under the terms of the GNU Lesser General Public
//  * License as published by the Free Software Foundation; either
//  * version 3 of the License, or (at your option) any later version.
//  *
//  * This program is distributed in the hope that it will be useful,
//  * but WITHOUT ANY WARRANTY; without even the implied warranty of
//  * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//  * Lesser General Public License for more details.
//  *
//  * You should have received a copy of the GNU Lesser General Public License
//  * along with this program; if not, write to the Free Software Foundation,
//  * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
//  */

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

[Export(typeof(ICodeActionStorage))]
[Export(typeof(ICodeActionStorageWriter))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]

internal class CodeActionStorage(IRoslynWorkspaceWrapper workspace, IThreadHandling threadHandling) : ICodeActionStorageWriter
{
    private readonly Dictionary<string, Dictionary<Guid, (CodeAction, Solution)>> storage = new();

    public void Clear(string filePath) => storage.Remove(filePath);

    public IRoslynQuickFix? GetCodeActionOrNull(string filePath, Guid id)
    {
        if (storage.TryGetValue(filePath, out var actions))
        {
            if (actions.TryGetValue(id, out var action))
            {
                return new RoslynQuickFix(workspace, action.Item2, action.Item1, threadHandling);
            }
        }
        return null;
    }

    public void Add(string filePath, Guid id, CodeAction action, Solution solution)
    {
        if (!storage.TryGetValue(filePath, out var actions))
        {
            storage[filePath] = actions = new Dictionary<Guid, (CodeAction, Solution)>();
        }

        actions[id] = (action, solution);
    }
}
