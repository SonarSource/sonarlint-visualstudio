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
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests.Roslyn;

[TestClass]
public class AnalyzerChangeTests
{
    private IRoslynSolutionWrapper solution;
    private AnalyzerFileReference analyzer1;
    private AnalyzerFileReference analyzer2;
    private AnalyzerFileReference analyzer3;
    private AnalyzerFileReference analyzer4;

    [TestInitialize]
    public void TestInitialize()
    {
        solution = Substitute.For<IRoslynSolutionWrapper>();

        analyzer1 = new AnalyzerFileReference(@"C:\abc1", Substitute.For<IAnalyzerAssemblyLoader>());
        analyzer2 = new AnalyzerFileReference(@"C:\abc2", Substitute.For<IAnalyzerAssemblyLoader>());
        analyzer3 = new AnalyzerFileReference(@"C:\abc3", Substitute.For<IAnalyzerAssemblyLoader>());
        analyzer4 = new AnalyzerFileReference(@"C:\abc4", Substitute.For<IAnalyzerAssemblyLoader>());
    }

    [TestMethod]
    public void Properties_SetToCorrectValues()
    {
        var analyzersToRemove = ImmutableArray.Create(analyzer1, analyzer2);
        var analyzersToAdd = ImmutableArray.Create(analyzer3, analyzer4);
        var testSubject = new AnalyzerChange(analyzersToRemove, analyzersToAdd);

        testSubject.AnalyzersToRemove.Should().BeEquivalentTo(analyzersToRemove);
        testSubject.AnalyzersToAdd.Should().BeEquivalentTo(analyzersToAdd);
    }

    [TestMethod]
    public void Change_PerformsUpdatesOnSolution()
    {
        var intermediateSolution = Substitute.For<IRoslynSolutionWrapper>();
        var resultingSolution = Substitute.For<IRoslynSolutionWrapper>();
        var allAnalyzersToRemove = ImmutableArray.Create(analyzer1, analyzer2);
        var allAnalyzersToAdd = ImmutableArray.Create(analyzer3, analyzer4);
        SetUpRemove(allAnalyzersToRemove, allAnalyzersToRemove, solution, intermediateSolution);
        SetUpAdd(allAnalyzersToAdd, allAnalyzersToAdd, intermediateSolution, resultingSolution);
        var testSubject = new AnalyzerChange(allAnalyzersToRemove, allAnalyzersToAdd);

        testSubject.Change(solution).Should().BeSameAs(resultingSolution);
    }

    [TestMethod]
    public void Change_FiltersOutAlreadyContainedOrRemovedAnalyzers()
    {
        var intermediateSolution = Substitute.For<IRoslynSolutionWrapper>();
        var resultingSolution = Substitute.For<IRoslynSolutionWrapper>();
        var allAnalyzersToRemove = ImmutableArray.Create(analyzer1, analyzer2);
        List<AnalyzerFileReference> actualAnalyzersToRemove = [analyzer1];
        var allAnalyzersToAdd = ImmutableArray.Create(analyzer3, analyzer4);
        List<AnalyzerFileReference> actualAnalyzersToAdd = [analyzer4];
        SetUpRemove(allAnalyzersToRemove, actualAnalyzersToRemove, solution, intermediateSolution);
        SetUpAdd(allAnalyzersToAdd, actualAnalyzersToAdd, intermediateSolution, resultingSolution);
        var testSubject = new AnalyzerChange(allAnalyzersToRemove, allAnalyzersToAdd);

        testSubject.Change(solution).Should().BeSameAs(resultingSolution);

        Received.InOrder(() =>
        {
            solution.ContainsAnalyzer(analyzer1);
            solution.ContainsAnalyzer(analyzer2);
            solution.RemoveAnalyzerReferences(Arg.Is<IReadOnlyCollection<AnalyzerFileReference>>(x => x.SequenceEqual(actualAnalyzersToRemove)));
            intermediateSolution.ContainsAnalyzer(analyzer3);
            intermediateSolution.ContainsAnalyzer(analyzer4);
            intermediateSolution.AddAnalyzerReferences(Arg.Is<IReadOnlyCollection<AnalyzerFileReference>>(x => x.SequenceEqual(actualAnalyzersToAdd)));
        });
    }

    [TestMethod]
    public void Change_RemovedAnalyzersAlreadyRemoved_SkipsRemoval()
    {
        var resultingSolution = Substitute.For<IRoslynSolutionWrapper>();
        var allAnalyzersToRemove = ImmutableArray.Create(analyzer1);
        SetUpRemove(allAnalyzersToRemove, [], solution, default);
        var allAnalyzersToAdd = ImmutableArray.Create(analyzer2);
        List<AnalyzerFileReference> actualAnalyzersToAdd = [analyzer2];
        SetUpAdd(allAnalyzersToAdd, actualAnalyzersToAdd, solution, resultingSolution);
        var testSubject = new AnalyzerChange(allAnalyzersToRemove,
            allAnalyzersToAdd);

        testSubject.Change(solution).Should().BeSameAs(resultingSolution);

        solution.DidNotReceiveWithAnyArgs().RemoveAnalyzerReferences(default);
    }


    [TestMethod]
    public void Change_NothingToRemove_SkipsRemoval()
    {
        var resultingSolution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpRemove(ImmutableArray<AnalyzerFileReference>.Empty, [], solution, default);
        var allAnalyzersToAdd = ImmutableArray.Create(analyzer2);
        List<AnalyzerFileReference> actualAnalyzersToAdd = [analyzer2];
        SetUpAdd(allAnalyzersToAdd, actualAnalyzersToAdd, solution, resultingSolution);
        var testSubject = new AnalyzerChange(ImmutableArray<AnalyzerFileReference>.Empty, allAnalyzersToAdd);

        testSubject.Change(solution).Should().BeSameAs(resultingSolution);

        solution.DidNotReceiveWithAnyArgs().RemoveAnalyzerReferences(default);
    }

    [TestMethod]
    public void Change_AddedAnalyzersAlreadyAdded_SkipsAddition()
    {
        var resultingSolution = Substitute.For<IRoslynSolutionWrapper>();
        var allAnalyzersToRemove = ImmutableArray.Create(analyzer1);
        SetUpRemove(allAnalyzersToRemove, [analyzer1], solution, resultingSolution);
        var allAnalyzersToAdd = ImmutableArray.Create(analyzer2);
        SetUpAdd(allAnalyzersToAdd, [], resultingSolution, default);

        var testSubject = new AnalyzerChange(allAnalyzersToRemove,
            allAnalyzersToAdd);

        testSubject.Change(solution).Should().BeSameAs(resultingSolution);

        solution.DidNotReceiveWithAnyArgs().AddAnalyzerReferences(default);
    }


    [TestMethod]
    public void Change_NothingToAdd_SkipsAddition()
    {
        var resultingSolution = Substitute.For<IRoslynSolutionWrapper>();
        var allAnalyzersToRemove = ImmutableArray.Create(analyzer1);
        SetUpRemove(allAnalyzersToRemove, [analyzer1], solution, resultingSolution);
        SetUpAdd(ImmutableArray<AnalyzerFileReference>.Empty, [], solution, default);
        var testSubject = new AnalyzerChange(allAnalyzersToRemove,
            ImmutableArray<AnalyzerFileReference>.Empty);

        testSubject.Change(solution).Should().BeSameAs(resultingSolution);

        solution.DidNotReceiveWithAnyArgs().AddAnalyzerReferences(default);
    }

    private static void SetUpAdd(
        ImmutableArray<AnalyzerFileReference> allAnalyzersToAdd,
        ICollection<AnalyzerFileReference> actualAnalyzersToAdd,
        IRoslynSolutionWrapper original,
        IRoslynSolutionWrapper resulting)
    {
        SetUpContains(allAnalyzersToAdd, allAnalyzersToAdd.Except(actualAnalyzersToAdd).ToList(), original);
        original.AddAnalyzerReferences(Arg.Is<IReadOnlyCollection<AnalyzerFileReference>>(x => x.SequenceEqual(actualAnalyzersToAdd))).Returns(resulting);
    }

    private static void SetUpRemove(
        ImmutableArray<AnalyzerFileReference> allAnalyzersToRemove,
        ICollection<AnalyzerFileReference> actualAnalyzersToRemove,
        IRoslynSolutionWrapper original,
        IRoslynSolutionWrapper resulting)
    {
        SetUpContains(allAnalyzersToRemove, actualAnalyzersToRemove, original);
        original.RemoveAnalyzerReferences(Arg.Is<IReadOnlyCollection<AnalyzerFileReference>>(x => x.SequenceEqual(actualAnalyzersToRemove))).Returns(resulting);
    }

    private static void SetUpContains(IEnumerable<AnalyzerFileReference> allAnalyzers, ICollection<AnalyzerFileReference> containedAnalyzers, IRoslynSolutionWrapper solutionWrapper)
    {
        foreach (var analyzer in allAnalyzers)
        {
            solutionWrapper.ContainsAnalyzer(analyzer).Returns(containedAnalyzers.Contains(analyzer));
        }
    }
}
