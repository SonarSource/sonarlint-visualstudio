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
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma;
using static SonarLint.VisualStudio.RoslynAnalyzerServer.IntegrationTests.Analysis.Pragma.PragmaTestHelper;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.IntegrationTests.Analysis.Pragma;

[TestClass]
public class PragmaWarningDisableCodeFixProviderTests
{
    [TestMethod]
    public async Task ApplyCodeFix_SingleIdPairedPragma_RemovesBothLines()
    {
        var source =
            """
            #pragma warning disable S1234
            class Foo { }
            #pragma warning restore S1234
            """;

        var diagnostics = await GetPragmaDiagnosticsForSourceAsync(source);
        var result = await ApplyCodeFixAsync(source, diagnostics[0]);

        Normalize(result).Should().Be(Normalize(
            """
            class Foo { }
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_MultiIdPairedPragma_RemovesOnlyUnmatchedId()
    {
        var source =
            """
            #pragma warning disable S1234, S5678
            class Foo {
            //SimulateIssue:S1234
            }
            #pragma warning restore S1234, S5678
            """;

        var diagnostics = await GetPragmaDiagnosticsForSourceAsync(source);
        var diagnostic = diagnostics.First(d =>
            d.Properties[DiagnosticAwarePragmaAnalyzer.ReportedDiagnosticId] == "S5678");
        var result = await ApplyCodeFixAsync(source, diagnostic);

        var expected =
            """
            #pragma warning disable S1234
            class Foo {
            //SimulateIssue:S1234
            }
            #pragma warning restore S1234
            """;
        Normalize(result).Should().Be(Normalize(expected));
    }

    [TestMethod]
    public async Task ApplyCodeFix_MultiIdPairedPragma_RestoreNonPaired_RemovesOnlyUnmatchedId()
    {
        var source =
            """
            #pragma warning disable S1234, S5678
            class Foo {
            //SimulateIssue:S1234
            }
            #pragma warning restore S5678
            """;

        var diagnostics = await GetPragmaDiagnosticsForSourceAsync(source);
        var diagnostic = diagnostics.First(d =>
            d.Properties[DiagnosticAwarePragmaAnalyzer.ReportedDiagnosticId] == "S5678");
        var result = await ApplyCodeFixAsync(source, diagnostic);

        var expected =
            """
            #pragma warning disable S1234
            class Foo {
            //SimulateIssue:S1234
            }
            """;
        Normalize(result).Should().Be(Normalize(expected));
    }

    [TestMethod]
    public async Task ApplyCodeFix_UnmatchedDisableOnly_RemovesLine()
    {
        var source =
            """
            #pragma warning disable S1234
            class Foo { }
            """;

        var diagnostics = await GetPragmaDiagnosticsForSourceAsync(source);
        var result = await ApplyCodeFixAsync(source, diagnostics[0]);

        Normalize(result).Should().Be(Normalize(
            """
            class Foo { }
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_UnmatchedRestoreOnly_RemovesLine()
    {
        var source =
            """
            class Foo { }
            #pragma warning restore S1234
            """;

        var diagnostics = await GetPragmaDiagnosticsForSourceAsync(source);
        var result = await ApplyCodeFixAsync(source, diagnostics[0]);

        Normalize(result).Should().Be(Normalize(
            """
            class Foo { }
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_ThreeIds_RemovesMiddleId()
    {
        var source =
            """
            #pragma warning disable S1234, S5678, S9999
            class Foo {
            //SimulateIssue:S1234
            //SimulateIssue:S9999
            }
            #pragma warning restore S1234, S5678, S9999
            """;

        var diagnostics = await GetPragmaDiagnosticsForSourceAsync(source);
        var diagnostic = diagnostics.First(d =>
            d.Properties[DiagnosticAwarePragmaAnalyzer.ReportedDiagnosticId] == "S5678");
        var result = await ApplyCodeFixAsync(source, diagnostic);

        var expected =
            """
            #pragma warning disable S1234, S9999
            class Foo {
            //SimulateIssue:S1234
            //SimulateIssue:S9999
            }
            #pragma warning restore S1234, S9999
            """;
        Normalize(result).Should().Be(Normalize(expected));
    }

    [TestMethod]
    public async Task ApplyCodeFix_PragmaBetweenSingleLineComments_RemovesPragmaPreservesComments()
    {
        var source =
            """
            // Comment above
            #pragma warning disable S1234
            // Comment below
            class Foo { }
            #pragma warning restore S1234
            """;

        var diagnostics = await GetPragmaDiagnosticsForSourceAsync(source);
        var result = await ApplyCodeFixAsync(source, diagnostics[0]);

        Normalize(result).Should().Be(Normalize(
            """
            // Comment above
            // Comment below
            class Foo { }
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_PragmaBetweenXmlDocAndClass_RemovesPragmaPreservesDoc()
    {
        var source =
            """
            /// <summary>Description of Foo</summary>
            #pragma warning disable S1234
            class Foo { }
            #pragma warning restore S1234
            """;

        var diagnostics = await GetPragmaDiagnosticsForSourceAsync(source);
        var result = await ApplyCodeFixAsync(source, diagnostics[0]);

        Normalize(result).Should().Be(Normalize(
            """
            /// <summary>Description of Foo</summary>
            class Foo { }
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_IndentedPragmaInsideNamespace_RemovesPragmaPreservesIndentation()
    {
        var source =
            """
            namespace TestNamespace
            {
                #pragma warning disable S1234
                class Foo { }
                #pragma warning restore S1234
            }
            """;

        var diagnostics = await GetPragmaDiagnosticsForSourceAsync(source);
        var result = await ApplyCodeFixAsync(source, diagnostics[0]);

        Normalize(result).Should().Be(Normalize(
            """
            namespace TestNamespace
            {
                class Foo { }
            }
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_AdjacentPragmas_RemovesOnlyUnnecessaryOuterPragma()
    {
        var source =
            """
            #pragma warning disable S1234
            #pragma warning disable S5678
            class Foo
            {
            //SimulateIssue:S5678
            }
            #pragma warning restore S5678
            #pragma warning restore S1234
            """;

        var diagnostics = await GetPragmaDiagnosticsForSourceAsync(source);
        var diagnostic = diagnostics.First();
        var result = await ApplyCodeFixAsync(source, diagnostic);

        Normalize(result).Should().Be(Normalize(
            """
            #pragma warning disable S5678
            class Foo
            {
            //SimulateIssue:S5678
            }
            #pragma warning restore S5678
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_AdjacentPragmas_WithExtraCode_RemovesOnlyUnnecessaryOuterPragma()
    {
        var source =
            """
            class Bar {}
            #pragma warning disable S1234
            #pragma warning disable S5678
            class Foo
            {
            //SimulateIssue:S5678
            }
            #pragma warning restore S5678
            #pragma warning restore S1234
            class Baz {}
            """;

        var diagnostics = await GetPragmaDiagnosticsForSourceAsync(source);
        var diagnostic = diagnostics.First();
        var result = await ApplyCodeFixAsync(source, diagnostic);

        Normalize(result).Should().Be(Normalize(
            """
            class Bar {}
            #pragma warning disable S5678
            class Foo
            {
            //SimulateIssue:S5678
            }
            #pragma warning restore S5678
            class Baz {}
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_AdjacentPragmas_RemovesOnlyUnnecessaryInnerPragma()
    {
        var source =
            """
            #pragma warning disable S1234
            #pragma warning disable S5678
            class Foo {
            //SimulateIssue:S1234
            }
            #pragma warning restore S5678
            #pragma warning restore S1234
            """;

        var diagnostics = await GetPragmaDiagnosticsForSourceAsync(source);
        var diagnostic = diagnostics.First();
        var result = await ApplyCodeFixAsync(source, diagnostic);

        Normalize(result).Should().Be(Normalize(
            """
            #pragma warning disable S1234
            class Foo {
            //SimulateIssue:S1234
            }
            #pragma warning restore S1234
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_PragmaBetweenBlockCommentAndCode_RemovesPragmaPreservesComment()
    {
        var source =
            """
            /* Block comment */
            #pragma warning disable S1234
            class Foo { }
            #pragma warning restore S1234
            """;

        var diagnostics = await GetPragmaDiagnosticsForSourceAsync(source);
        var result = await ApplyCodeFixAsync(source, diagnostics[0]);

        Normalize(result).Should().Be(Normalize(
            """
            /* Block comment */
            class Foo { }
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_StackedPragmas_RemovesMiddlePragmaPreservesOthers()
    {
        var source =
            """
            #pragma warning disable S1111
            #pragma warning disable S2222
            #pragma warning disable S3333
            class Foo {
                //SimulateIssue:S1111
                //SimulateIssue:S3333
            }
            #pragma warning restore S3333
            #pragma warning restore S2222
            #pragma warning restore S1111
            """;

        var diagnostics = await GetPragmaDiagnosticsForSourceAsync(source);
        var diagnostic = diagnostics.First(d =>
            d.Properties[DiagnosticAwarePragmaAnalyzer.ReportedDiagnosticId] == "S2222");
        var result = await ApplyCodeFixAsync(source, diagnostic);

        Normalize(result).Should().Be(Normalize(
            """
            #pragma warning disable S1111
            #pragma warning disable S3333
            class Foo {
                //SimulateIssue:S1111
                //SimulateIssue:S3333
            }
            #pragma warning restore S3333
            #pragma warning restore S1111
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_PragmaInsideRegion_RemovesPragmaPreservesRegion()
    {
        var source =
            """
            #region MyRegion
            #pragma warning disable S1234
            class Foo { }
            #pragma warning restore S1234
            #endregion
            """;

        var diagnostics = await GetPragmaDiagnosticsForSourceAsync(source);
        var result = await ApplyCodeFixAsync(source, diagnostics[0]);

        Normalize(result).Should().Be(Normalize(
            """
            #region MyRegion
            class Foo { }
            #endregion
            """));
    }

    [TestMethod]
    public async Task ApplyCodeFix_MultiIdPragmaBetweenComments_RemovesIdPreservesComments()
    {
        var source =
            """
            // Comment above
            #pragma warning disable S1234, S5678
            // Comment below
            class Foo {
            //SimulateIssue:S1234
            }
            // Comment above restore
            #pragma warning restore S1234, S5678
            // Comment below restore
            """;

        var diagnostics = await GetPragmaDiagnosticsForSourceAsync(source);
        var diagnostic = diagnostics.First(d =>
            d.Properties[DiagnosticAwarePragmaAnalyzer.ReportedDiagnosticId] == "S5678");
        var result = await ApplyCodeFixAsync(source, diagnostic);

        Normalize(result).Should().Be(Normalize(
            """
            // Comment above
            #pragma warning disable S1234
            // Comment below
            class Foo {
            //SimulateIssue:S1234
            }
            // Comment above restore
            #pragma warning restore S1234
            // Comment below restore
            """));
    }

    private static async Task<ImmutableArray<Diagnostic>> GetPragmaDiagnosticsForSourceAsync(string source)
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(source);
        return await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);
    }

    private static async Task<string> ApplyCodeFixAsync(string source, Diagnostic diagnostic) =>
        await PragmaTestHelper.ApplyCodeFixAsync(source, diagnostic, new PragmaWarningDisableCodeFixProvider());
}
