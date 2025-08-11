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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
public class SonarRoslynFileSyntaxAnalysisTests
{
    private const string TestFilePath = "test.cs";

    private TestLogger logger;
    private ISonarRoslynCompilationWithAnalyzersWrapper compilationWrapper;
    private SyntaxTree syntaxTree;
    private SonarRoslynFileSyntaxAnalysis testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.ForPartsOf<TestLogger>();
        compilationWrapper = Substitute.For<ISonarRoslynCompilationWithAnalyzersWrapper>();
        syntaxTree = Substitute.For<SyntaxTree>();

        testSubject = new SonarRoslynFileSyntaxAnalysis(TestFilePath);
    }

    [TestMethod]
    public void Constructor_SetsProperties() =>
        testSubject.AnalysisFilePath.Should().Be(TestFilePath);

    [TestMethod]
    public async Task ExecuteAsync_SyntaxTreeExists_ReturnsSyntaxDiagnostics()
    {
        var expectedDiagnostics = ImmutableArray.Create(CreateTestDiagnostic("id1"), CreateTestDiagnostic("id2"));
        compilationWrapper.GetSyntaxTree(TestFilePath).Returns(syntaxTree);
        compilationWrapper.GetAnalyzerSyntaxDiagnosticsAsync(syntaxTree, Arg.Any<CancellationToken>()).Returns(expectedDiagnostics);

        var result = await testSubject.ExecuteAsync(compilationWrapper, CancellationToken.None);

        result.Should().BeEquivalentTo(expectedDiagnostics);
    }

    [TestMethod]
    public async Task ExecuteAsync_NoSyntaxTree_ReturnsEmptyArray()
    {
        compilationWrapper.GetSyntaxTree(TestFilePath).ReturnsNull();

        var result = await testSubject.ExecuteAsync(compilationWrapper, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ExecuteAsync_CancellationTokenPassed_UsesTokenForDiagnostics()
    {
        var expectedToken = new CancellationToken(true);
        compilationWrapper.GetSyntaxTree(TestFilePath).Returns(syntaxTree);

        await testSubject.ExecuteAsync(compilationWrapper, expectedToken);

        await compilationWrapper.Received(1).GetAnalyzerSyntaxDiagnosticsAsync(
            syntaxTree,
            Arg.Is<CancellationToken>(token => token.Equals(expectedToken)));
    }

    private static Diagnostic CreateTestDiagnostic(string id)
    {
        var descriptor = new DiagnosticDescriptor(
            id,
            "title",
            "message",
            "category",
            DiagnosticSeverity.Warning,
            true);

        var location = Location.Create(
            "test.cs",
            new TextSpan(0, 1),
            new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 1)));

        return Diagnostic.Create(descriptor, location);
    }
}
