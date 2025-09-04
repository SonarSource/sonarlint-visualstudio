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
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

[Export(typeof(IRoslynQuickFixFactory))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method:ImportingConstructor]
internal class RoslynQuickFixFactory(IRoslynWorkspaceWrapper workspace, IRoslynCodeActionFactory roslynCodeActionFactory, IRoslynQuickFixStorageWriter quickFixStorage)
    : IRoslynQuickFixFactory
{
    public async Task<List<RoslynQuickFix>> CreateQuickFixesAsync(
        Diagnostic diagnostic,
        IRoslynSolutionWrapper solution,
        RoslynAnalysisConfiguration analysisConfiguration,
        CancellationToken token)
    {
        var quickFixes = new List<RoslynQuickFix>();

        if (analysisConfiguration.CodeFixProvidersByRuleKey.TryGetValue(diagnostic.Id, out var availableCodeFixProviders)
            && solution.GetDocument(diagnostic.Location.SourceTree) is {} document
            &&  await roslynCodeActionFactory.GetCodeActionsAsync(availableCodeFixProviders, diagnostic, document, token) is {} codeActions)
        {
            foreach (var codeAction in codeActions)
            {
                var id = Guid.NewGuid();
                quickFixStorage.Add(id, new RoslynQuickFixApplicationImpl(workspace, solution, codeAction));
                quickFixes.Add(new RoslynQuickFix(id));
            }
        }

        return quickFixes;
    }
}
