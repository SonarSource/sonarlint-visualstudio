/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma;
using static SonarLint.VisualStudio.RoslynAnalyzerServer.IntegrationTests.Analysis.Pragma.PragmaTestHelper;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.IntegrationTests.Analysis.Pragma;

[TestClass]
public class PragmaWarningGenerateCodeFixProviderTests
{
    [TestMethod]
    public async Task ApplyCodeFix_SimpleClass_WrapWithPragmaDisableRestore()
    {
        var source =
            """
            class Foo { }
            """;

        var diagnostic = CreateDiagnosticForToken(source, "class", "S1234");
        var result = await ApplyCodeFixAsync(source, diagnostic, new PragmaWarningGenerateCodeFixProvider());

        Normalize(result).Should().Be(Normalize(
            """
            #pragma warning disable S1234
            class Foo { }
            #pragma warning restore S1234
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_IndentedCodeInsideNamespace_PreservesIndentation()
    {
        var source =
            """
            namespace TestNamespace
            {
                class Foo { }
            }
            """;

        var diagnostic = CreateDiagnosticForToken(source, "class", "S1234");
        var result = await ApplyCodeFixAsync(source, diagnostic, new PragmaWarningGenerateCodeFixProvider());

        Normalize(result).Should().Be(Normalize(
            """
            namespace TestNamespace
            {
            #pragma warning disable S1234
                class Foo { }
            #pragma warning restore S1234
            }
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_CodeWithLeadingComment_CommentStaysAbovePragma()
    {
        var source =
            """
            // Comment above
            class Foo { }
            """;

        var diagnostic = CreateDiagnosticForToken(source, "class", "S1234");
        var result = await ApplyCodeFixAsync(source, diagnostic, new PragmaWarningGenerateCodeFixProvider());

        Normalize(result).Should().Be(Normalize(
            """
            // Comment above
            #pragma warning disable S1234
            class Foo { }
            #pragma warning restore S1234
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_MultipleStatements_WrapsOnlyDiagnosticLine()
    {
        var source =
            """
            class Foo { }
            class Bar { }
            """;

        var diagnostic = CreateDiagnosticForToken(source, "Foo", "S1234");
        var result = await ApplyCodeFixAsync(source, diagnostic, new PragmaWarningGenerateCodeFixProvider());

        Normalize(result).Should().Be(Normalize(
            """
            #pragma warning disable S1234
            class Foo { }
            #pragma warning restore S1234
            class Bar { }
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_DiagnosticOnSingleTokenLine_WrapsCorrectly()
    {
        var source =
            """
            class Foo
            {
            }
            """;

        var diagnostic = CreateDiagnosticForToken(source, "}", "S1234");
        var result = await ApplyCodeFixAsync(source, diagnostic, new PragmaWarningGenerateCodeFixProvider());

        Normalize(result).Should().Be(Normalize(
            """
            class Foo
            {
            #pragma warning disable S1234
            }
            #pragma warning restore S1234
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_MultiLineStatement_WrapsEntireStatement()
    {
        var source =
            """
            class Foo
            {
                void M()
                {
                    var x =
                        42;
                }
            }
            """;

        var diagnostic = CreateDiagnosticForToken(source, "x", "S1234");
        var result = await ApplyCodeFixAsync(source, diagnostic, new PragmaWarningGenerateCodeFixProvider());

        Normalize(result).Should().Be(Normalize(
            """
            class Foo
            {
                void M()
                {
            #pragma warning disable S1234
                    var x =
                        42;
            #pragma warning restore S1234
                }
            }
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_PreprocessorDirectiveBeforeCode_PragmaPlacedCorrectly()
    {
        var source =
            """
            #region MyRegion
            class Foo { }
            #endregion
            """;

        var diagnostic = CreateDiagnosticForToken(source, "class", "S1234");
        var result = await ApplyCodeFixAsync(source, diagnostic, new PragmaWarningGenerateCodeFixProvider());

        Normalize(result).Should().Be(Normalize(
            """
            #region MyRegion
            #pragma warning disable S1234
            class Foo { }
            #pragma warning restore S1234
            #endregion
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_ArrowExpressionClause_WrapsArrowClause()
    {
        var source =
            """
            class Foo
            {
                int Value => 42;
            }
            """;

        var diagnostic = CreateDiagnosticForToken(source, "42", "S1234");
        var result = await ApplyCodeFixAsync(source, diagnostic, new PragmaWarningGenerateCodeFixProvider());

        Normalize(result).Should().Be(Normalize(
            """
            class Foo
            {
            #pragma warning disable S1234
                int Value => 42;
            #pragma warning restore S1234
            }
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_DiagnosticAtEndOfFileNoTrailingNewline_RestorePlacedCorrectly()
    {
        var source = "class Foo { }";

        var diagnostic = CreateDiagnosticForToken(source, "class", "S1234");
        var result = await ApplyCodeFixAsync(source, diagnostic, new PragmaWarningGenerateCodeFixProvider());

        Normalize(result).Should().Be(Normalize(
            """
            #pragma warning disable S1234
            class Foo { }
            #pragma warning restore S1234
            """));
    }

    [TestMethod]
    public async Task RegisterCodeFixesAsync_FileLevelIssue_DoesNotRegisterCodeFix()
    {
        var source =
            """
            class Foo { }
            """;

        var diagnostic = CreateDiagnostic("S1234", CSharpSyntaxTree.ParseText(source), new TextSpan(0, 0));

        var (_, _, actions) = await GetCodeFixActionsAsync(source, diagnostic, new PragmaWarningGenerateCodeFixProvider());

        actions.Should().BeEmpty();
    }

    private static Diagnostic CreateDiagnosticForToken(string source, string tokenText, string ruleId)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();
        var token = root.DescendantTokens().First(t => t.Text == tokenText);
        return CreateDiagnostic(ruleId, tree, token.Span);
    }
}
