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
using SonarLint.VisualStudio.Integration.TestInfrastructure;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests;

[TestClass]
public class RoslynQuickFixApplicationImplTests
{
    private IRoslynWorkspaceWrapper workspace = null!;
    private IRoslynSolutionWrapper originalSolution = null!;
    private IRoslynCodeActionWrapper codeAction = null!;
    private CancellationToken cancellationToken;
    private RoslynQuickFixApplicationImpl testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        cancellationToken = new CancellationToken();
        workspace = Substitute.For<IRoslynWorkspaceWrapper>();
        originalSolution = Substitute.For<IRoslynSolutionWrapper>();
        codeAction = Substitute.For<IRoslynCodeActionWrapper>();

        testSubject = new RoslynQuickFixApplicationImpl(workspace, originalSolution, codeAction);
    }

    [TestMethod]
    public void Message_ReturnsCodeActionTitle()
    {
        const string title = "Test title";
        codeAction.Title.Returns(title);

        testSubject.Message.Should().Be(title);
    }

    [TestMethod]
    public async Task ApplyAsync_NoApplyChangesOperation_DoesNotApplyChanges()
    {
        codeAction.GetOperationsAsync(cancellationToken).Returns(ImmutableArray.Create(Substitute.For<CodeActionOperation>()));

        var result = await testSubject.ApplyAsync(cancellationToken);

        VerifyNotAppliedChanges(result);
    }

    [TestMethod]
    public async Task ApplyAsync_MultipleOperationsWithApplyChangesOperation_DoesNotApplyChanges()
    {
        var operations = ImmutableArray.Create(new Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation(CreateDummySolution()), Substitute.For<CodeActionOperation>());
        codeAction.GetOperationsAsync(cancellationToken).Returns(operations);

        var result = await testSubject.ApplyAsync(cancellationToken);

        VerifyNotAppliedChanges(result);
    }

    [TestMethod]
    public async Task ApplyAsync_HasApplyChangesOperation_CallsWorkspaceApplyChanges()
    {
        var applyChangesOperation = new Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation(CreateDummySolution());
        var operations = ImmutableArray.Create<CodeActionOperation>(applyChangesOperation);
        codeAction.GetOperationsAsync(cancellationToken).Returns(operations);
        workspace.ApplyOrMergeChangesAsync(originalSolution, applyChangesOperation, cancellationToken).Returns(true);

        var result = await testSubject.ApplyAsync(cancellationToken);

        result.Should().BeTrue();
        await workspace.Received(1).ApplyOrMergeChangesAsync(originalSolution, applyChangesOperation, cancellationToken);
    }

    [TestMethod]
    public async Task ApplyAsync_WorkspaceApplyChangesFails_ReturnsFalse()
    {
        var applyChangesOperation = new Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation(CreateDummySolution());
        var operations = ImmutableArray.Create<CodeActionOperation>(applyChangesOperation);
        codeAction.GetOperationsAsync(cancellationToken).Returns(operations);
        workspace.ApplyOrMergeChangesAsync(originalSolution, applyChangesOperation, cancellationToken).Returns(false);

        var result = await testSubject.ApplyAsync(cancellationToken);

        result.Should().BeFalse();
        await workspace.Received(1).ApplyOrMergeChangesAsync(originalSolution, applyChangesOperation, cancellationToken);
    }

    private void VerifyNotAppliedChanges(bool result)
    {
        result.Should().BeFalse();
        workspace.DidNotReceiveWithAnyArgs().ApplyOrMergeChangesAsync(default!, default!, default).IgnoreAwaitForAssert();
    }

    private Solution CreateDummySolution()
    {
        var adhocWorkspace = new AdhocWorkspace();

        var slnInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default, null,
            []);
        adhocWorkspace.AddSolution(slnInfo);

        return adhocWorkspace.CurrentSolution;
    }
}
