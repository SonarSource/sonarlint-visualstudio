/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests.Roslyn;

[TestClass]
public class RoslynWrappersTests
{
    private IRoslynWorkspaceWrapper roslynWorkspaceWrapper;

    [TestInitialize]
    public void TestInitialize()
    {
        roslynWorkspaceWrapper = CreateWorkspaceWrapper(new NoOpThreadHandler());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<RoslynWorkspaceWrapper>();
    }
    
    [TestMethod]
    public void AddAnalyzer_CurrentSolutionContainsAnalyzer()
    {
        var analyzerFileReference = new AnalyzerFileReference(@"C:\abc", Substitute.For<IAnalyzerAssemblyLoader>());
        var analyzers = ImmutableArray.Create(analyzerFileReference);

        var solutionAfterAddition = roslynWorkspaceWrapper.CurrentSolution.AddAnalyzerReferences(analyzers);
        roslynWorkspaceWrapper.TryApplyChanges(solutionAfterAddition).Should().BeTrue();
        
        roslynWorkspaceWrapper.CurrentSolution.GetRoslynSolution().AnalyzerReferences.Should().Contain(analyzerFileReference);
    }
    
    [TestMethod]
    public void AddExtraAnalyzer_CurrentSolutionContainsBothAnalyzers()
    {
        var analyzerFileReference = new AnalyzerFileReference(@"C:\abc", Substitute.For<IAnalyzerAssemblyLoader>());
        var extraAnalyzerFileReference = new AnalyzerFileReference(@"C:\abc", Substitute.For<IAnalyzerAssemblyLoader>());
        var extraAnalyzers = ImmutableArray.Create(extraAnalyzerFileReference);
        roslynWorkspaceWrapper.TryApplyChanges(roslynWorkspaceWrapper.CurrentSolution.AddAnalyzerReferences(ImmutableArray.Create(analyzerFileReference))).Should().BeTrue();
        
        var solutionAfterAddition = roslynWorkspaceWrapper.CurrentSolution.AddAnalyzerReferences(extraAnalyzers);
        roslynWorkspaceWrapper.TryApplyChanges(solutionAfterAddition).Should().BeTrue();

        var solutionAnalyzers = roslynWorkspaceWrapper.CurrentSolution.GetRoslynSolution().AnalyzerReferences;
        solutionAnalyzers.Should().Contain(analyzerFileReference);
        solutionAnalyzers.Should().Contain(extraAnalyzerFileReference);
    }
    
    [TestMethod]
    public void AddAndRemoveAnalyzer_CurrentSolutionNoLongerContainsAnalyzer()
    {
        var analyzerFileReference = new AnalyzerFileReference(@"C:\abc", Substitute.For<IAnalyzerAssemblyLoader>());
        var analyzers = ImmutableArray.Create(analyzerFileReference);
        
        var solutionAfterAddition = roslynWorkspaceWrapper.CurrentSolution.AddAnalyzerReferences(analyzers);
        roslynWorkspaceWrapper.TryApplyChanges(solutionAfterAddition).Should().BeTrue();
        
        var solutionAfterRemoval = roslynWorkspaceWrapper.CurrentSolution.RemoveAnalyzerReferences(analyzers);
        roslynWorkspaceWrapper.TryApplyChanges(solutionAfterRemoval).Should().BeTrue();
        
        roslynWorkspaceWrapper.CurrentSolution.GetRoslynSolution().AnalyzerReferences.Should().NotContain(analyzerFileReference);
    }
    
    [TestMethod]
    public void RemoveAnalyzer_IsNotPresentInTheCurrentSolution_AppliesNoChange()
    {
        var analyzerFileReference = new AnalyzerFileReference(@"C:\abc", Substitute.For<IAnalyzerAssemblyLoader>());
        var analyzers = ImmutableArray.Create(analyzerFileReference);
        
        var solutionAfterRemoval = roslynWorkspaceWrapper.CurrentSolution.RemoveAnalyzerReferences(analyzers);
        roslynWorkspaceWrapper.TryApplyChanges(solutionAfterRemoval).Should().BeTrue();
        
        roslynWorkspaceWrapper.CurrentSolution.GetRoslynSolution().AnalyzerReferences.Should().NotContain(analyzerFileReference);
    }
    
    [TestMethod]
    public void AddMultipleAndRemoveOneAnalyzer_CurrentSolutionContainsOneAnalyzer()
    {
        var analyzerFileReference1 = new AnalyzerFileReference(@"C:\abc", Substitute.For<IAnalyzerAssemblyLoader>());
        var analyzerFileReference2 = new AnalyzerFileReference(@"C:\abc", Substitute.For<IAnalyzerAssemblyLoader>());
        var analyzersToAdd = ImmutableArray.Create(analyzerFileReference1, analyzerFileReference2);
        var analyzersToRemove = ImmutableArray.Create(analyzerFileReference1);
        
        roslynWorkspaceWrapper.TryApplyChanges(roslynWorkspaceWrapper.CurrentSolution.AddAnalyzerReferences(analyzersToAdd)).Should().BeTrue();
        roslynWorkspaceWrapper.TryApplyChanges(roslynWorkspaceWrapper.CurrentSolution.RemoveAnalyzerReferences(analyzersToRemove)).Should().BeTrue();
        roslynWorkspaceWrapper.CurrentSolution.GetRoslynSolution().AnalyzerReferences.Should().NotContain(analyzerFileReference1);
        roslynWorkspaceWrapper.CurrentSolution.GetRoslynSolution().AnalyzerReferences.Should().Contain(analyzerFileReference2);
    }

    [TestMethod]
    public void TryApplyChanges_RunsOnUIThread()
    {
        var threadHandling = Substitute.For<IThreadHandling>();
        var testSubject = CreateWorkspaceWrapper(threadHandling);

        testSubject.TryApplyChanges(testSubject.CurrentSolution);

        threadHandling.Received(1).RunOnUIThread(Arg.Any<Action>());
    }

    private static IRoslynWorkspaceWrapper CreateWorkspaceWrapper(IThreadHandling threadHandling)
    {
        var adhocWorkspace = new AdhocWorkspace();

        var slnInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default, null, []);
        adhocWorkspace.AddSolution(slnInfo);

        return new RoslynWorkspaceWrapper(adhocWorkspace, threadHandling);
    }
}
