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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.IntegrationTests.Analysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TestBadClassNameAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TEST001";
    public const string InvalidFilePath = @"C:\TestProject\Invalid.cs";
    public const string InvalidFileErrorMessage = "This file is not supposed to be analyzed, rule (de)activation does not work";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Bad class name",
        "Class name '{0}' contains 'Bad'",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        if (context.Node.SyntaxTree.FilePath == InvalidFilePath)
        {
            throw new NotSupportedException(InvalidFileErrorMessage);
        }

        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var className = classDeclaration.Identifier.Text;

        if (className.Contains("Bad"))
        {
            var diagnostic = Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(), className);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
