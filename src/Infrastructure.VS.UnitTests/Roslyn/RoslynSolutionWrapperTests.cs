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
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests.Roslyn;

[TestClass]
public class RoslynWrappersTests
{
    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<RoslynWorkspaceWrapper>();
    }
    
    [TestMethod]
    public void AddAnalyzer_CurrentSolutionNoLongerContainsAnalyzer()
    {
        var analyzerFileReference = new AnalyzerFileReference(@"C:\abc", Substitute.For<IAnalyzerAssemblyLoader>());
        var roslynWorkspaceWrapper = CreateWorkspaceWrapper();
        var analyzers = ImmutableArray.Create(analyzerFileReference);

        var solutionAfterAddition = roslynWorkspaceWrapper.CurrentSolution.WithAnalyzerReferences(analyzers);
        roslynWorkspaceWrapper.TryApplyChanges(solutionAfterAddition).Should().BeTrue();
        
        roslynWorkspaceWrapper.CurrentSolution.GetRoslynSolution().AnalyzerReferences.Contains(analyzerFileReference).Should().BeTrue();
    }
    
    [TestMethod]
    public void AddAndRemoveAnalyzer_CurrentSolutionNoLongerContainsAnalyzer()
    {
        var analyzerFileReference = new AnalyzerFileReference(@"C:\abc", Substitute.For<IAnalyzerAssemblyLoader>());
        var roslynWorkspaceWrapper = CreateWorkspaceWrapper();
        var analyzers = ImmutableArray.Create(analyzerFileReference);
        
        var solutionAfterAddition = roslynWorkspaceWrapper.CurrentSolution.WithAnalyzerReferences(analyzers);
        roslynWorkspaceWrapper.TryApplyChanges(solutionAfterAddition).Should().BeTrue();
        
        var solutionAfterRemoval = roslynWorkspaceWrapper.CurrentSolution.RemoveAnalyzerReferences(analyzers);
        roslynWorkspaceWrapper.TryApplyChanges(solutionAfterRemoval).Should().BeTrue();
        
        roslynWorkspaceWrapper.CurrentSolution.GetRoslynSolution().AnalyzerReferences.Contains(analyzerFileReference).Should().BeFalse();
    }
    
    [TestMethod]
    public void RemoveAnalyzer_IsNotPresentInTheCurrentSolution_AppliesNoChange()
    {
        var analyzerFileReference = new AnalyzerFileReference(@"C:\abc", Substitute.For<IAnalyzerAssemblyLoader>());
        var analyzers = ImmutableArray.Create(analyzerFileReference);
        var roslynWorkspaceWrapper = CreateWorkspaceWrapper();
        
        var solutionAfterRemoval = roslynWorkspaceWrapper.CurrentSolution.RemoveAnalyzerReferences(analyzers);
        roslynWorkspaceWrapper.TryApplyChanges(solutionAfterRemoval).Should().BeTrue();
        
        roslynWorkspaceWrapper.CurrentSolution.GetRoslynSolution().AnalyzerReferences.Contains(analyzerFileReference).Should().BeFalse();
    }
    
    [TestMethod]
    public void AddMultipleAndRemoveOneAnalyzer_CurrentSolutionContainsOneAnalyzer()
    {
        var analyzerFileReference1 = new AnalyzerFileReference(@"C:\abc", Substitute.For<IAnalyzerAssemblyLoader>());
        var analyzerFileReference2 = new AnalyzerFileReference(@"C:\abc", Substitute.For<IAnalyzerAssemblyLoader>());
        var analyzersToAdd = ImmutableArray.Create(analyzerFileReference1, analyzerFileReference2);
        var analyzersToRemove = ImmutableArray.Create(analyzerFileReference1);
        var roslynWorkspaceWrapper = CreateWorkspaceWrapper();
        
        roslynWorkspaceWrapper.TryApplyChanges(roslynWorkspaceWrapper.CurrentSolution.WithAnalyzerReferences(analyzersToAdd)).Should().BeTrue();
        roslynWorkspaceWrapper.TryApplyChanges(roslynWorkspaceWrapper.CurrentSolution.RemoveAnalyzerReferences(analyzersToRemove)).Should().BeTrue();
        roslynWorkspaceWrapper.CurrentSolution.GetRoslynSolution().AnalyzerReferences.Contains(analyzerFileReference1).Should().BeFalse();
        roslynWorkspaceWrapper.CurrentSolution.GetRoslynSolution().AnalyzerReferences.Contains(analyzerFileReference2).Should().BeTrue();
    }

    private static IRoslynWorkspaceWrapper CreateWorkspaceWrapper()
    {
        var adhocWorkspace = new AdhocWorkspace();

        var slnInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default, null, []);
        adhocWorkspace.AddSolution(slnInfo);

        return new RoslynWorkspaceWrapper(adhocWorkspace);
    }
}
