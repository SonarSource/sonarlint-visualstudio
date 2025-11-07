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
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.Integration.TestInfrastructure;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
public class RoslynQuickFixFactoryTests
{
    private IRoslynWorkspaceWrapper workspaceWrapper = null!;
    private IRoslynCodeActionFactory roslynCodeActionFactory = null!;
    private IRoslynQuickFixStorageWriter quickFixStorage = null!;
    private IRoslynSolutionWrapper solution = null!;
    private Diagnostic diagnostic = null!;
    private IRoslynDocumentWrapper document = null!;
    private IReadOnlyCollection<CodeFixProvider> codeFixProviders = null!;
    private CancellationToken token;
    private RoslynQuickFixFactory testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        codeFixProviders = [Substitute.For<CodeFixProvider>(), Substitute.For<CodeFixProvider>()];
        document = Substitute.For<IRoslynDocumentWrapper>();
        diagnostic = CreateDiagnostic("rule1");
        workspaceWrapper = Substitute.For<IRoslynWorkspaceWrapper>();
        roslynCodeActionFactory = Substitute.For<IRoslynCodeActionFactory>();
        quickFixStorage = Substitute.For<IRoslynQuickFixStorageWriter>();
        solution = Substitute.For<IRoslynSolutionWrapper>();
        token = CancellationToken.None;

        testSubject = new RoslynQuickFixFactory(workspaceWrapper, roslynCodeActionFactory, quickFixStorage);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynQuickFixFactory, IRoslynQuickFixFactory>(
            MefTestHelpers.CreateExport<IRoslynWorkspaceWrapper>(),
            MefTestHelpers.CreateExport<IRoslynCodeActionFactory>(),
            MefTestHelpers.CreateExport<IRoslynQuickFixStorageWriter>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RoslynQuickFixFactory>();

    [TestMethod]
    public async Task CreateQuickFixesAsync_NoCodeFixProvidersForRule_ReturnsEmptyList()
    {
        var result = await testSubject.CreateQuickFixesAsync(diagnostic, solution, CreateAnalysisConfiguration([]), token);

        result.Should().BeEmpty();
        roslynCodeActionFactory.DidNotReceiveWithAnyArgs().GetCodeActionsAsync(default!, default!, default!, default).IgnoreAwaitForAssert();
        quickFixStorage.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [TestMethod]
    public async Task CreateQuickFixesAsync_NoDocumentFound_ReturnsEmptyList()
    {
        var analysisConfiguration = CreateAnalysisConfiguration(new() { { diagnostic.Id, codeFixProviders } });
        solution.GetDocument(diagnostic.Location.SourceTree).ReturnsNull();

        var result = await testSubject.CreateQuickFixesAsync(diagnostic, solution, analysisConfiguration, token);

        result.Should().BeEmpty();
        roslynCodeActionFactory.DidNotReceiveWithAnyArgs().GetCodeActionsAsync(default!, default!, default!, default).IgnoreAwaitForAssert();
        quickFixStorage.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [TestMethod]
    public async Task CreateQuickFixesAsync_NoCodeActionsFound_ReturnsEmptyList()
    {
        var analysisConfiguration = CreateAnalysisConfiguration(new() { { diagnostic.Id, codeFixProviders } });
        solution.GetDocument(diagnostic.Location.SourceTree).Returns(document);
        roslynCodeActionFactory.GetCodeActionsAsync(default!, default!, default!, default)
            .ReturnsForAnyArgs([]);

        var result = await testSubject.CreateQuickFixesAsync(diagnostic, solution, analysisConfiguration, token);

        result.Should().BeEmpty();
        roslynCodeActionFactory.Received(1).GetCodeActionsAsync(codeFixProviders, diagnostic, document, token).IgnoreAwaitForAssert();
        quickFixStorage.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [TestMethod]
    public async Task CreateQuickFixesAsync_WithCodeActions_ReturnsQuickFixesAndAddsToStorage()
    {
        var codeAction1 = Substitute.For<IRoslynCodeActionWrapper>();
        var codeAction2 = Substitute.For<IRoslynCodeActionWrapper>();
        var codeAction3 = Substitute.For<IRoslynCodeActionWrapper>();
        var analysisConfiguration = CreateAnalysisConfiguration(new() { { diagnostic.Id, codeFixProviders } });
        solution.GetDocument(diagnostic.Location.SourceTree).Returns(document);
        roslynCodeActionFactory.GetCodeActionsAsync(Arg.Any<IReadOnlyCollection<CodeFixProvider>>(), diagnostic, document, token)
            .Returns([codeAction1, codeAction2, codeAction3]);

        var result = await testSubject.CreateQuickFixesAsync(diagnostic, solution, analysisConfiguration, token);

        result.Should().HaveCount(3);
        roslynCodeActionFactory.Received(1).GetCodeActionsAsync(codeFixProviders, diagnostic, document, token).IgnoreAwaitForAssert();
        quickFixStorage.Received(1).Add(Arg.Is<RoslynQuickFixApplicationImpl>(x => x.RoslynCodeAction == codeAction1));
        quickFixStorage.Received(1).Add(Arg.Is<RoslynQuickFixApplicationImpl>(x => x.RoslynCodeAction == codeAction2));
        quickFixStorage.Received(1).Add(Arg.Is<RoslynQuickFixApplicationImpl>(x => x.RoslynCodeAction == codeAction3));
    }

    private static Diagnostic CreateDiagnostic(string id)
    {
        var descriptor = new DiagnosticDescriptor(
            id,
            "any",
            "any",
            "any",
            DiagnosticSeverity.Warning,
            true);

        var location = Location.Create(
            "any",
            new TextSpan(0, 1),
            new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 1)));

        return Diagnostic.Create(descriptor, location);
    }

    private static RoslynAnalysisConfiguration CreateAnalysisConfiguration(
        Dictionary<string, IReadOnlyCollection<CodeFixProvider>> codeFixProvidersByRuleKey) =>
        new(null!,
            null!,
            default,
            codeFixProvidersByRuleKey.ToImmutableDictionary());
}
