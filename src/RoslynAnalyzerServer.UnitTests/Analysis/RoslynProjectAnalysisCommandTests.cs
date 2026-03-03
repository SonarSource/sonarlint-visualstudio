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
using Microsoft.CodeAnalysis.Text;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
public class RoslynProjectAnalysisCommandTests
{
    private const string File1 = @"C:\project\file1.cs";
    private const string File2 = @"C:\project\file2.cs";
    private const string NonTargetFile = @"C:\project\other.cs";

    private IRoslynCompilationWithAnalyzersWrapper compilationWrapper = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        compilationWrapper = Substitute.For<IRoslynCompilationWithAnalyzersWrapper>();
    }

    [TestMethod]
    public void Constructor_SetsTargetFilePaths()
    {
        var testSubject = new RoslynProjectAnalysisCommand([File1, File2]);

        testSubject.TargetFilePaths.Should().BeEquivalentTo(File1, File2);
    }

    [TestMethod]
    public async Task ExecuteAsync_ReturnsDiagnosticsOnTargetFiles()
    {
        var diag1 = CreateTestDiagnostic("S001", File1);
        var diag2 = CreateTestDiagnostic("S002", File1);
        SetupGetAllDiagnostics(diag1, diag2);
        var testSubject = new RoslynProjectAnalysisCommand([File1]);

        var result = await testSubject.ExecuteAsync(compilationWrapper, CancellationToken.None);

        result.Should().BeEquivalentTo([diag1, diag2]);
    }

    [TestMethod]
    public async Task ExecuteAsync_MultipleTargetFiles_ReturnsDiagnosticsForAll()
    {
        var diag1 = CreateTestDiagnostic("S001", File1);
        var diag2 = CreateTestDiagnostic("S002", File2);
        SetupGetAllDiagnostics(diag1, diag2);
        var testSubject = new RoslynProjectAnalysisCommand([File1, File2]);

        var result = await testSubject.ExecuteAsync(compilationWrapper, CancellationToken.None);

        result.Should().BeEquivalentTo([diag1, diag2]);
    }

    [TestMethod]
    public async Task ExecuteAsync_DiagnosticOnNonTargetTree_IsFilteredOut()
    {
        var targetDiagnostic = CreateTestDiagnostic("S001", File1);
        var nonTargetDiagnostic = CreateTestDiagnostic("S002", NonTargetFile);
        SetupGetAllDiagnostics(targetDiagnostic, nonTargetDiagnostic);
        var testSubject = new RoslynProjectAnalysisCommand([File1]);

        var result = await testSubject.ExecuteAsync(compilationWrapper, CancellationToken.None);

        result.Should().BeEquivalentTo([targetDiagnostic]);
    }

    [TestMethod]
    public async Task ExecuteAsync_DiagnosticWithNoSourceTree_IsFilteredOut()
    {
        var targetDiagnostic = CreateTestDiagnostic("S001", File1);
        var noLocationDiagnostic = Diagnostic.Create("S003", "category", "message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 1);
        SetupGetAllDiagnostics(targetDiagnostic, noLocationDiagnostic);
        var testSubject = new RoslynProjectAnalysisCommand([File1]);

        var result = await testSubject.ExecuteAsync(compilationWrapper, CancellationToken.None);

        result.Should().BeEquivalentTo([targetDiagnostic]);
    }

    [TestMethod]
    public async Task ExecuteAsync_NoDiagnostics_ReturnsEmpty()
    {
        SetupGetAllDiagnostics();
        var testSubject = new RoslynProjectAnalysisCommand([File1]);

        var result = await testSubject.ExecuteAsync(compilationWrapper, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ExecuteAsync_CancellationTokenPassed_UsesToken()
    {
        var expectedToken = new CancellationToken(true);
        SetupGetAllDiagnostics();
        var testSubject = new RoslynProjectAnalysisCommand([File1]);

        await testSubject.ExecuteAsync(compilationWrapper, expectedToken);

        await compilationWrapper.Received(1).GetAllDiagnosticsAsync(
            Arg.Is<CancellationToken>(token => token.Equals(expectedToken)));
    }

    private void SetupGetAllDiagnostics(params Diagnostic[] diagnostics) =>
        compilationWrapper.GetAllDiagnosticsAsync(Arg.Any<CancellationToken>())
            .Returns(ImmutableArray.Create(diagnostics));

    private static Diagnostic CreateTestDiagnostic(string id, string filePath)
    {
        var descriptor = new DiagnosticDescriptor(
            id,
            "title",
            "message",
            "category",
            DiagnosticSeverity.Warning,
            true);

        var syntaxTree = CSharpSyntaxTree.ParseText("class C { }", path: filePath);
        var location = Location.Create(syntaxTree, new TextSpan(0, 1));

        return Diagnostic.Create(descriptor, location);
    }
}
