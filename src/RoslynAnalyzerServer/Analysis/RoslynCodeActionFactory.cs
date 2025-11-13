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

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

[Export(typeof(IRoslynCodeActionFactory))]
[PartCreationPolicy(CreationPolicy.Shared)]
[ExcludeFromCodeCoverage]
internal class RoslynCodeActionFactory : IRoslynCodeActionFactory
{
    public async Task<List<IRoslynCodeActionWrapper>> GetCodeActionsAsync(IReadOnlyCollection<CodeFixProvider> codeFixProviders, Diagnostic diagnostic, IRoslynDocumentWrapper document, CancellationToken token)
    {
        var codeActions = new List<IRoslynCodeActionWrapper>();
        foreach (var codeFixProvider in codeFixProviders)
        {
            await codeFixProvider.RegisterCodeFixesAsync(new CodeFixContext(document.RoslynDocument, diagnostic, (c, _) => codeActions.Add(new RoslynCodeActionWrapper(c)), token));
        }
        return codeActions;
    }
}
