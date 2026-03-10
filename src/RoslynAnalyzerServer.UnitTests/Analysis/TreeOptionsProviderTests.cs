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
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
public class TreeOptionsProviderTests
{
    private const string TargetFilePath = @"C:\project\target.cs";
    private const string NonTargetFilePath = @"C:\project\other.cs";
    private const string KnownRuleId = "S1234";
    private const string UnknownRuleId = "S9999";

    private ImmutableDictionary<string, ReportDiagnostic> diagnosticOptions = null!;
    private TreeOptionsProvider testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        diagnosticOptions = ImmutableDictionary<string, ReportDiagnostic>.Empty
            .Add(KnownRuleId, ReportDiagnostic.Warn);
        testSubject = new TreeOptionsProvider(diagnosticOptions, [TargetFilePath]);
    }

    [TestMethod]
    public void TryGetDiagnosticValue_TargetTree_KnownRule_ReturnsConfiguredSeverity()
    {
        var tree = CreateSyntaxTree(TargetFilePath);

        var result = testSubject.TryGetDiagnosticValue(tree, KnownRuleId, CancellationToken.None, out var severity);

        result.Should().BeTrue();
        severity.Should().Be(ReportDiagnostic.Warn);
    }

    [TestMethod]
    public void TryGetDiagnosticValue_TargetTree_UnknownRule_ReturnsSuppressed()
    {
        var tree = CreateSyntaxTree(TargetFilePath);

        var result = testSubject.TryGetDiagnosticValue(tree, UnknownRuleId, CancellationToken.None, out var severity);

        result.Should().BeTrue();
        severity.Should().Be(ReportDiagnostic.Suppress);
    }

    [TestMethod]
    public void TryGetDiagnosticValue_NonTargetTree_KnownRule_ReturnsSuppressed()
    {
        var tree = CreateSyntaxTree(NonTargetFilePath);

        var result = testSubject.TryGetDiagnosticValue(tree, KnownRuleId, CancellationToken.None, out var severity);

        result.Should().BeTrue();
        severity.Should().Be(ReportDiagnostic.Suppress);
    }

    [TestMethod]
    public void TryGetDiagnosticValue_NonTargetTree_UnknownRule_ReturnsSuppressed()
    {
        var tree = CreateSyntaxTree(NonTargetFilePath);

        var result = testSubject.TryGetDiagnosticValue(tree, UnknownRuleId, CancellationToken.None, out var severity);

        result.Should().BeTrue();
        severity.Should().Be(ReportDiagnostic.Suppress);
    }

    [TestMethod]
    public void TryGetGlobalDiagnosticValue_ReturnsFalseWithDefault()
    {
        var result = testSubject.TryGetGlobalDiagnosticValue(KnownRuleId, CancellationToken.None, out var severity);

        result.Should().BeFalse();
        severity.Should().Be(ReportDiagnostic.Default);
    }

    [TestMethod]
    public void IsGenerated_ReturnsUnknown()
    {
        var tree = CreateSyntaxTree(TargetFilePath);

        var result = testSubject.IsGenerated(tree, CancellationToken.None);

        result.Should().Be(GeneratedKind.Unknown);
    }

    private static SyntaxTree CreateSyntaxTree(string filePath)
    {
        var tree = Substitute.For<SyntaxTree>();
        tree.FilePath.Returns(filePath);
        return tree;
    }
}
