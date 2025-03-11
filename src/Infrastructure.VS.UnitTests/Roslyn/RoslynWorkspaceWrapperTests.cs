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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests.Roslyn;

[TestClass]
public class RoslynWorkspaceWrapperTests
{
    private IThreadHandling threadHandling;
    private AdhocWorkspace currentWorkspace;
    private IAnalyzerChange analyzerChange;
    private RoslynWorkspaceWrapper testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        threadHandling = Substitute.For<IThreadHandling>();
        threadHandling.RunOnUIThreadAsync(Arg.Any<Action>()).ReturnsForAnyArgs(info =>
        {
            (info[0] as Action)?.Invoke();
            return Task.CompletedTask;
        });
        currentWorkspace = new AdhocWorkspace();
        var slnInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default, null, []);
        currentWorkspace.AddSolution(slnInfo);
        analyzerChange = Substitute.For<IAnalyzerChange>();
        testSubject = new RoslynWorkspaceWrapper(currentWorkspace, threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RoslynWorkspaceWrapper>();

    [TestMethod]
    public async Task TryApplyChangesAsync_Success_UpdatesWorkspace()
    {
        var originalSolution = currentWorkspace.CurrentSolution;
        var updatedSolutionWrapper = Substitute.For<IRoslynSolutionWrapper>();
        var solutionThatSuccessfullyUpdates = GetSolutionThatSuccessfullyUpdates(originalSolution);
        updatedSolutionWrapper.RoslynSolution.Returns(solutionThatSuccessfullyUpdates);
        analyzerChange.Change(Arg.Is<IRoslynSolutionWrapper>(x => x.RoslynSolution == originalSolution)).Returns(updatedSolutionWrapper);

        var result = await testSubject.TryApplyChangesAsync(analyzerChange);

        result.Should().NotBeNull();
        result.RoslynSolution.Should().NotBe(originalSolution);
        result.RoslynSolution.Should().BeSameAs(currentWorkspace.CurrentSolution);
        AssertCurrentSolutionIsEquivalentTo(updatedSolutionWrapper);
        CheckRunChangeOnUIThread();
    }

    [TestMethod]
    public async Task TryApplyChangesAsync_NoUpdate_ReturnsOriginal()
    {
        var originalSolution = currentWorkspace.CurrentSolution;
        analyzerChange.Change(Arg.Is<IRoslynSolutionWrapper>(x => x.RoslynSolution == originalSolution)).Returns(info => info[0] as IRoslynSolutionWrapper);

        var result = await testSubject.TryApplyChangesAsync(analyzerChange);

        result.Should().NotBeNull();
        result.RoslynSolution.Should().BeSameAs(originalSolution);
        testSubject.CurrentSolution.RoslynSolution.Should().BeSameAs(originalSolution);
        CheckRunChangeOnUIThread();
    }

    [TestMethod]
    public async Task TryApplyChangesAsync_UpdateFails_RetriesAndFails()
    {
        var originalSolution = currentWorkspace.CurrentSolution;
        var failed = new RoslynSolutionWrapper(GetSolutionThatFailsUpdate());
        analyzerChange.Change(Arg.Is<IRoslynSolutionWrapper>(x => x.RoslynSolution == originalSolution)).Returns(failed);

        var result = await testSubject.TryApplyChangesAsync(analyzerChange);

        result.Should().BeNull();
        testSubject.CurrentSolution.RoslynSolution.Should().BeSameAs(originalSolution);

        Received.InOrder(() =>
        {
            for (var i = 0; i < 5; i++)
            {
                threadHandling.RunOnUIThreadAsync(Arg.Any<Action>());
                analyzerChange.Change(Arg.Any<IRoslynSolutionWrapper>());
            }
        });
    }

    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataTestMethod]
    public async Task TryApplyChangesAsync(int failedUpdateTimes)
    {
        var originalSolution = currentWorkspace.CurrentSolution;
        var failed = new RoslynSolutionWrapper(GetSolutionThatFailsUpdate());
        var success = new RoslynSolutionWrapper(GetSolutionThatSuccessfullyUpdates(originalSolution));
        analyzerChange.Change(Arg.Is<IRoslynSolutionWrapper>(x => x.RoslynSolution == originalSolution)).Returns(
            failed,
            Enumerable.Repeat(failed, failedUpdateTimes - 1).Append(success).ToArray());

        var result = await testSubject.TryApplyChangesAsync(analyzerChange);

        result.Should().NotBeNull();
        result.RoslynSolution.Should().NotBe(originalSolution);
        result.RoslynSolution.Should().BeSameAs(currentWorkspace.CurrentSolution);
        AssertCurrentSolutionIsEquivalentTo(success);

        Received.InOrder(() =>
        {
            for (var i = 0; i < failedUpdateTimes + 1; i++)
            {
                threadHandling.RunOnUIThreadAsync(Arg.Any<Action>());
                analyzerChange.Change(Arg.Any<IRoslynSolutionWrapper>());
            }
        });
    }

    [TestMethod]
    public void CurrentSolution_WorkspaceUpdated_ReturnsUpToDateValue()
    {
        var originalSolution = currentWorkspace.CurrentSolution;
        var solutionThatSuccessfullyUpdates = GetSolutionThatSuccessfullyUpdates(originalSolution);
        currentWorkspace.TryApplyChanges(solutionThatSuccessfullyUpdates).Should().BeTrue();
        var updatedSolution = currentWorkspace.CurrentSolution;

        var result = testSubject.CurrentSolution.RoslynSolution;

        result.Should().NotBeSameAs(originalSolution);
        result.Should().BeSameAs(updatedSolution);
    }

    private void CheckRunChangeOnUIThread() =>
        Received.InOrder(() =>
        {
            threadHandling.RunOnUIThreadAsync(Arg.Any<Action>());
            analyzerChange.Change(Arg.Any<IRoslynSolutionWrapper>());
        });

    private static Solution GetSolutionThatFailsUpdate() => new AdhocWorkspace().AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default, null, []));

    private static Solution GetSolutionThatSuccessfullyUpdates(Solution originalSolution)
    {
        var analyzerAssemblyLoader = Substitute.For<IAnalyzerAssemblyLoader>();
        var analyzerFileReference = new AnalyzerFileReference(@"C:\some\file", analyzerAssemblyLoader);
        var updatedSolution = originalSolution.AddAnalyzerReference(analyzerFileReference);
        return updatedSolution;
    }

    private void AssertCurrentSolutionIsEquivalentTo(IRoslynSolutionWrapper solution) =>
        solution.Should().NotBeNull().And.BeAssignableTo<IRoslynSolutionWrapper>().Subject.RoslynSolution.AnalyzerReferences.Should()
            .BeEquivalentTo(testSubject.CurrentSolution.RoslynSolution.AnalyzerReferences);
}
