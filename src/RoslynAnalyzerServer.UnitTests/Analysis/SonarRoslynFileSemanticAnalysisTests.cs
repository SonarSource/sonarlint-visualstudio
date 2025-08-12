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
public class SonarRoslynFileSemanticAnalysisTests
{
    private const string TestFilePath = "c:\\test\\file.cs";
    private ISonarRoslynCompilationWithAnalyzersWrapper compilationWrapper = null!;
    private TestLogger testLogger = null!;
    private SonarRoslynFileSemanticAnalysis testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        testLogger = Substitute.ForPartsOf<TestLogger>();
        compilationWrapper = Substitute.For<ISonarRoslynCompilationWithAnalyzersWrapper>();

        testSubject = new SonarRoslynFileSemanticAnalysis(TestFilePath, testLogger);
    }

    [TestMethod]
    public void MefCtor() =>
        testSubject.AnalysisFilePath.Should().Be(TestFilePath);

    [TestMethod]
    public async Task ExecuteAsync_SemanticModelIsNull_ReturnsEmptyCollection()
    {
        compilationWrapper.GetSemanticModel(TestFilePath).ReturnsNull();

        var result = await testSubject.ExecuteAsync(compilationWrapper, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ExecuteAsync_SemanticModelExists_ReturnsAnalyzerDiagnostics()
    {
        var semanticModel = Substitute.For<SemanticModel>();
        var expectedDiagnostics = ImmutableArray.Create(CreateTestDiagnostic("id1"),  CreateTestDiagnostic("id2"), CreateTestDiagnostic("id3"));
        compilationWrapper.GetSemanticModel(TestFilePath).Returns(semanticModel);
        compilationWrapper.GetAnalyzerSemanticDiagnosticsAsync(semanticModel, CancellationToken.None)
            .Returns(expectedDiagnostics);

        var result = await testSubject.ExecuteAsync(compilationWrapper, CancellationToken.None);

        result.Should().BeEquivalentTo(expectedDiagnostics);
    }

    [TestMethod]
    public async Task ExecuteAsync_CancellationTokenPassed_UsesTokenForDiagnostics()
    {
        var semanticModel = Substitute.For<SemanticModel>();
        var cancellationToken = new CancellationToken(true);
        compilationWrapper.GetSemanticModel(TestFilePath).Returns(semanticModel);

        await testSubject.ExecuteAsync(compilationWrapper, cancellationToken);

        await compilationWrapper.Received(1).GetAnalyzerSemanticDiagnosticsAsync(semanticModel, cancellationToken);
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
