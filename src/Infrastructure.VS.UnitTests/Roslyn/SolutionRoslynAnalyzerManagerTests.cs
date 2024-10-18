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
using NSubstitute.ClearExtensions;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests.Roslyn;

[TestClass]
public class SolutionRoslynAnalyzerManagerTests
{
    private IEmbeddedRoslynAnalyzerProvider embeddedRoslynAnalyzerProvider;
    private IConnectedModeRoslynAnalyzerProvider connectedModeRoslynAnalyzerProvider;
    private IRoslynWorkspaceWrapper roslynWorkspaceWrapper;
    private IEqualityComparer<ImmutableArray<AnalyzerFileReference>?> analyzerComparer;
    private SolutionRoslynAnalyzerManager testSubject;
    private ImmutableArray<AnalyzerFileReference> embeddedAnalyzers = ImmutableArray.Create(new AnalyzerFileReference(@"C:\path", Substitute.For<IAnalyzerAssemblyLoader>()));
    private ImmutableArray<AnalyzerFileReference> connectedAnalyzers = ImmutableArray.Create(new AnalyzerFileReference(@"C:\path", Substitute.For<IAnalyzerAssemblyLoader>()));
    private BindingConfiguration connectedModeConfiguration = new(
        new BoundServerProject(
            "local",
            "server",
            new ServerConnection.SonarCloud("org")),
        SonarLintMode.Connected,
        "path");

    [TestInitialize]
    public void TestInitialize()
    {
        embeddedRoslynAnalyzerProvider = Substitute.For<IEmbeddedRoslynAnalyzerProvider>();
        connectedModeRoslynAnalyzerProvider = Substitute.For<IConnectedModeRoslynAnalyzerProvider>();
        roslynWorkspaceWrapper = Substitute.For<IRoslynWorkspaceWrapper>();
        analyzerComparer = Substitute.For<IEqualityComparer<ImmutableArray<AnalyzerFileReference>?>>();
        testSubject = new SolutionRoslynAnalyzerManager(embeddedRoslynAnalyzerProvider,
            connectedModeRoslynAnalyzerProvider,
            roslynWorkspaceWrapper,
            analyzerComparer);
    }
    
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SolutionRoslynAnalyzerManager, ISolutionRoslynAnalyzerManager>(
            MefTestHelpers.CreateExport<IEmbeddedRoslynAnalyzerProvider>(),
            MefTestHelpers.CreateExport<IConnectedModeRoslynAnalyzerProvider>(),
            MefTestHelpers.CreateExport<IRoslynWorkspaceWrapper>());
    }
    
    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SolutionRoslynAnalyzerManager>();
    }

    [TestMethod]
    public void Ctor_SubscribesToConnectedModeAnalyzerProviderEvent()
    {
        var connectedModeAnalyzerProvider = Substitute.For<IConnectedModeRoslynAnalyzerProvider>();
        new SolutionRoslynAnalyzerManager(embeddedRoslynAnalyzerProvider,
            connectedModeAnalyzerProvider,
            roslynWorkspaceWrapper,
            analyzerComparer);

        connectedModeAnalyzerProvider.Received().AnalyzerUpdatedForConnection += Arg.Any<EventHandler<AnalyzerUpdatedForConnectionEventArgs>>();
    }
    
    [TestMethod]
    public void OnSolutionChanged_NoOpenSolution_DoesNothing()
    {
        testSubject.OnSolutionChanged(null, BindingConfiguration.Standalone);

        roslynWorkspaceWrapper.ReceivedCalls().Should().BeEmpty();
    }
    
    [TestMethod]
    public void OnSolutionChanged_StandaloneSolution_AppliesEmbeddedAnalyzer()
    {
        embeddedRoslynAnalyzerProvider.Get()
            .Returns(embeddedAnalyzers);
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v2Solution = Substitute.For<IRoslynSolutionWrapper>();
        roslynWorkspaceWrapper.CurrentSolution.Returns(v1Solution);
        v1Solution.WithAnalyzerReferences(embeddedAnalyzers).Returns(v2Solution);
        roslynWorkspaceWrapper.TryApplyChanges(v2Solution).Returns(true);
        
        testSubject.OnSolutionChanged("solution", BindingConfiguration.Standalone);

        roslynWorkspaceWrapper.Received().TryApplyChanges(v2Solution);
    }
    
    [TestMethod]
    public void OnSolutionChanged_StandaloneSolution_BindingSet_RemovesStandaloneAndAppliesConnectedAnalyzer()
    {
        const string solutionName = "solution";
        embeddedRoslynAnalyzerProvider.Get().Returns(embeddedAnalyzers);
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v2Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v3Solution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpStandaloneSolution(v1Solution, solutionName);

        analyzerComparer.Equals(embeddedAnalyzers, connectedAnalyzers).Returns(false);
        connectedModeRoslynAnalyzerProvider.GetOrNull(connectedModeConfiguration.Project.ServerConnection).Returns(connectedAnalyzers);
        roslynWorkspaceWrapper.CurrentSolution.Returns(v1Solution, v2Solution);
        v1Solution.RemoveAnalyzerReferences(embeddedAnalyzers).Returns(v2Solution);
        v2Solution.WithAnalyzerReferences(connectedAnalyzers).Returns(v3Solution);
        roslynWorkspaceWrapper.TryApplyChanges(v2Solution).Returns(true);
        roslynWorkspaceWrapper.TryApplyChanges(v3Solution).Returns(true);
        
        testSubject.OnSolutionChanged(solutionName, connectedModeConfiguration);

        Received.InOrder(() =>
        {
            analyzerComparer.Equals(embeddedAnalyzers, connectedAnalyzers);
            v1Solution.RemoveAnalyzerReferences(embeddedAnalyzers);
            roslynWorkspaceWrapper.TryApplyChanges(v2Solution);
            v2Solution.WithAnalyzerReferences(connectedAnalyzers);
            roslynWorkspaceWrapper.TryApplyChanges(v3Solution);
        });
    }
    
    [TestMethod]
    public void OnSolutionChanged_StandaloneSolution_BindingSet_NoConnectedAnalyzer_DoesNothing()
    {
        const string solutionName = "solution";
        embeddedRoslynAnalyzerProvider.Get().Returns(embeddedAnalyzers);
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpStandaloneSolution(v1Solution, solutionName);

        analyzerComparer.Equals(embeddedAnalyzers, embeddedAnalyzers).Returns(true);
        connectedModeRoslynAnalyzerProvider.GetOrNull(connectedModeConfiguration.Project.ServerConnection).Returns((ImmutableArray<AnalyzerFileReference>?)null);
        testSubject.OnSolutionChanged(solutionName, connectedModeConfiguration);

        Received.InOrder(() =>
        {
            connectedModeRoslynAnalyzerProvider.GetOrNull(connectedModeConfiguration.Project.ServerConnection);
            embeddedRoslynAnalyzerProvider.Get();
            analyzerComparer.Equals(embeddedAnalyzers, embeddedAnalyzers);
        });
        roslynWorkspaceWrapper.DidNotReceiveWithAnyArgs().TryApplyChanges(default);
    }
    
    [TestMethod]
    public void OnSolutionChanged_SolutionClosedAndReopened_RegistersAnalyzersAgain()
    {
        embeddedRoslynAnalyzerProvider.Get()
            .Returns(embeddedAnalyzers);
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v2Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v3Solution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpStandaloneSolution(v1Solution, "solution");
        embeddedRoslynAnalyzerProvider.Get().Returns(embeddedAnalyzers);
        roslynWorkspaceWrapper.CurrentSolution.Returns(v2Solution);
        v2Solution.WithAnalyzerReferences(embeddedAnalyzers).Returns(v3Solution);
        roslynWorkspaceWrapper.TryApplyChanges(v3Solution).Returns(true);
        
        testSubject.OnSolutionChanged(null, BindingConfiguration.Standalone);
        testSubject.OnSolutionChanged("solution", BindingConfiguration.Standalone);
        
        v2Solution.Received().WithAnalyzerReferences(embeddedAnalyzers);
        roslynWorkspaceWrapper.Received().TryApplyChanges(v3Solution);
    }
    
    [TestMethod]
    public void OnSolutionChanged_SolutionClosedAndReopenedAsBound_RegistersConnectedAnalyzers()
    {
        embeddedRoslynAnalyzerProvider.Get()
            .Returns(embeddedAnalyzers);
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v2Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v3Solution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpStandaloneSolution(v1Solution, "solution");
        embeddedRoslynAnalyzerProvider.Get().Returns(embeddedAnalyzers);
        connectedModeRoslynAnalyzerProvider.GetOrNull(connectedModeConfiguration.Project.ServerConnection).Returns(connectedAnalyzers);
        roslynWorkspaceWrapper.CurrentSolution.Returns(v2Solution);
        v2Solution.WithAnalyzerReferences(connectedAnalyzers).Returns(v3Solution);
        roslynWorkspaceWrapper.TryApplyChanges(v3Solution).Returns(true);
        
        testSubject.OnSolutionChanged(null, BindingConfiguration.Standalone);
        testSubject.OnSolutionChanged("solution", connectedModeConfiguration);
        
        v2Solution.Received().WithAnalyzerReferences(connectedAnalyzers);
        roslynWorkspaceWrapper.Received().TryApplyChanges(v3Solution);
    }
    
    [TestMethod]
    public void OnSolutionChanged_DifferentSolutionOpened_RegistersAnalyzers()
    {
        const string solutionName = "solution";
        embeddedRoslynAnalyzerProvider.Get().Returns(embeddedAnalyzers);
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v2Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v3Solution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpStandaloneSolution(v1Solution, solutionName);

        connectedModeRoslynAnalyzerProvider.GetOrNull(connectedModeConfiguration.Project.ServerConnection).Returns(connectedAnalyzers);
        roslynWorkspaceWrapper.CurrentSolution.Returns(v2Solution);
        v2Solution.WithAnalyzerReferences(connectedAnalyzers).Returns(v3Solution);
        roslynWorkspaceWrapper.TryApplyChanges(v3Solution).Returns(true);
        
        testSubject.OnSolutionChanged("different solution", connectedModeConfiguration);

        Received.InOrder(() =>
        {
            v2Solution.WithAnalyzerReferences(connectedAnalyzers);
            roslynWorkspaceWrapper.TryApplyChanges(v3Solution);
        });
    }

    private void SetUpStandaloneSolution(IRoslynSolutionWrapper solution, string solutionName)
    {
        embeddedRoslynAnalyzerProvider.Get().Returns(embeddedAnalyzers);
        var sourceSolution = Substitute.For<IRoslynSolutionWrapper>();
        sourceSolution.WithAnalyzerReferences(embeddedAnalyzers).Returns(solution);
        roslynWorkspaceWrapper.CurrentSolution.Returns(sourceSolution);
        roslynWorkspaceWrapper.TryApplyChanges(solution).Returns(true);
        testSubject.OnSolutionChanged(solutionName, BindingConfiguration.Standalone);
        analyzerComparer.ClearReceivedCalls();
        embeddedRoslynAnalyzerProvider.ClearReceivedCalls();
        roslynWorkspaceWrapper.ClearReceivedCalls();
    }
    
    private void SetUpBoundSolution(IRoslynSolutionWrapper solution, string solutionName)
    {
        var sourceSolution = Substitute.For<IRoslynSolutionWrapper>();
        sourceSolution.WithAnalyzerReferences(connectedAnalyzers).Returns(solution);
        roslynWorkspaceWrapper.CurrentSolution.Returns(sourceSolution);
        roslynWorkspaceWrapper.TryApplyChanges(solution).Returns(true);
        testSubject.OnSolutionChanged(solutionName, connectedModeConfiguration);
        embeddedRoslynAnalyzerProvider.ClearSubstitute();
        analyzerComparer.ClearReceivedCalls();
        roslynWorkspaceWrapper.ClearReceivedCalls();
    }
}
