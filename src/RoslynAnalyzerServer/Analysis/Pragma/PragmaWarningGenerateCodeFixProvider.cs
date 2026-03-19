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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma.Roslyn;
using Document = Microsoft.CodeAnalysis.Document;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class PragmaWarningGenerateCodeFixProvider : CodeFixProvider
{
    private const string EquivalenceKeyPrefix = "SonarLint.PragmaGenerate.";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray<string>.Empty; // manually added as code fix for all rules

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    string.Format(Resources.PragmaWarningGenerateCodeFixTitle, diagnostic.Id),
                    ct => AbstractSuppressionCodeFixProvider.AddPragmaDirectivesAsync(context.Document, root, diagnostic, ct),
                    equivalenceKey: EquivalenceKeyPrefix + diagnostic.Id),
                diagnostic);
        }
    }
}
