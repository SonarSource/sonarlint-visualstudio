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
using SonarLint.VisualStudio.Core;
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
    private ILogger logger;
    private SolutionRoslynAnalyzerManager testSubject;
    private readonly ImmutableArray<AnalyzerFileReference> embeddedAnalyzers = ImmutableArray.Create(new AnalyzerFileReference(@"C:\path\embedded", Substitute.For<IAnalyzerAssemblyLoader>()));
    private readonly ImmutableArray<AnalyzerFileReference> connectedAnalyzers = ImmutableArray.Create(new AnalyzerFileReference(@"C:\path\connected", Substitute.For<IAnalyzerAssemblyLoader>()));
    private readonly BindingConfiguration connectedModeConfiguration = new(
        new BoundServerProject(
            "local",
            "server",
            new ServerConnection.SonarCloud("org")),
        SonarLintMode.Connected,
        "path");

    [TestInitialize]
    public void TestInitialize()
    {
        logger = new TestLogger();
        embeddedRoslynAnalyzerProvider = Substitute.For<IEmbeddedRoslynAnalyzerProvider>();
        connectedModeRoslynAnalyzerProvider = Substitute.For<IConnectedModeRoslynAnalyzerProvider>();
        roslynWorkspaceWrapper = Substitute.For<IRoslynWorkspaceWrapper>();
        analyzerComparer = Substitute.For<IEqualityComparer<ImmutableArray<AnalyzerFileReference>?>>();
        testSubject = new SolutionRoslynAnalyzerManager(
            embeddedRoslynAnalyzerProvider,
            connectedModeRoslynAnalyzerProvider,
            roslynWorkspaceWrapper,
            analyzerComparer,
            logger);
    }
    
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SolutionRoslynAnalyzerManager, ISolutionRoslynAnalyzerManager>(
            MefTestHelpers.CreateExport<IEmbeddedRoslynAnalyzerProvider>(),
            MefTestHelpers.CreateExport<IConnectedModeRoslynAnalyzerProvider>(),
            MefTestHelpers.CreateExport<IRoslynWorkspaceWrapper>(),
            MefTestHelpers.CreateExport<ILogger>());
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
        new SolutionRoslynAnalyzerManager(
            embeddedRoslynAnalyzerProvider,
            connectedModeAnalyzerProvider,
            roslynWorkspaceWrapper,
            analyzerComparer,
            logger);

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
        SetUpCurrentSolutionSequence(v1Solution);
        SetUpAnalyzerAddition(v1Solution, v2Solution, embeddedAnalyzers);
        
        testSubject.OnSolutionChanged("solution", BindingConfiguration.Standalone);

        roslynWorkspaceWrapper.Received().TryApplyChanges(v2Solution);
    }

    [TestMethod]
    public void OnSolutionChanged_StandaloneSolution_BindingSet_RemovesStandaloneAndAppliesConnectedAnalyzer()
    {
        const string solutionName = "solution";
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v2Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v3Solution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpStandaloneSolution(v1Solution, solutionName);

        analyzerComparer.Equals(embeddedAnalyzers, connectedAnalyzers).Returns(false);
        connectedModeRoslynAnalyzerProvider.GetOrNull(connectedModeConfiguration.Project.ServerConnection).Returns(connectedAnalyzers);
        roslynWorkspaceWrapper.CurrentSolution.Returns(v1Solution, v2Solution);
        SetUpAnalyzerRemoval(v1Solution, v2Solution, embeddedAnalyzers);
        SetUpAnalyzerAddition(v2Solution, v3Solution, connectedAnalyzers);
        
        testSubject.OnSolutionChanged(solutionName, connectedModeConfiguration);

        Received.InOrder(() =>
        {
            analyzerComparer.Equals(embeddedAnalyzers, connectedAnalyzers);
            v1Solution.RemoveAnalyzerReferences(embeddedAnalyzers);
            roslynWorkspaceWrapper.TryApplyChanges(v2Solution);
            v2Solution.AddAnalyzerReferences(connectedAnalyzers);
            roslynWorkspaceWrapper.TryApplyChanges(v3Solution);
        });
        embeddedRoslynAnalyzerProvider.DidNotReceiveWithAnyArgs().Get();
    }
    
    [TestMethod]
    public void OnSolutionChanged_StandaloneSolution_BindingSet_NoConnectedAnalyzer_DoesNotReRegisterEmbedded()
    {
        const string solutionName = "solution";
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpStandaloneSolution(v1Solution, solutionName);
        EnableDefaultEmbeddedAnalyzers();
        connectedModeRoslynAnalyzerProvider.GetOrNull(connectedModeConfiguration.Project.ServerConnection).Returns((ImmutableArray<AnalyzerFileReference>?)null);
        analyzerComparer.Equals(embeddedAnalyzers, embeddedAnalyzers).Returns(true);
        
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
        const string solutionName = "solution";
        var preCloseSolution = Substitute.For<IRoslynSolutionWrapper>();
        var v2Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v3Solution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpStandaloneSolution(preCloseSolution, solutionName);
        EnableDefaultEmbeddedAnalyzers();
        SetUpCurrentSolutionSequence(v2Solution);
        SetUpAnalyzerAddition(v2Solution, v3Solution, embeddedAnalyzers);
        
        testSubject.OnSolutionChanged(null, BindingConfiguration.Standalone);
        testSubject.OnSolutionChanged(solutionName, BindingConfiguration.Standalone);
        
        v2Solution.Received().AddAnalyzerReferences(embeddedAnalyzers);
        roslynWorkspaceWrapper.Received().TryApplyChanges(v3Solution);
    }
    
    [TestMethod]
    public void OnSolutionChanged_SolutionClosedAndReopenedAsBound_RegistersConnectedAnalyzers()
    {
        const string solutionName = "solution";
        var preCloseSolution = Substitute.For<IRoslynSolutionWrapper>();
        var v2Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v3Solution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpStandaloneSolution(preCloseSolution, solutionName);
        EnableDefaultEmbeddedAnalyzers();
        connectedModeRoslynAnalyzerProvider.GetOrNull(connectedModeConfiguration.Project.ServerConnection).Returns(connectedAnalyzers);
        SetUpAnalyzerAddition(v2Solution, v3Solution, connectedAnalyzers);
        
        testSubject.OnSolutionChanged(null, BindingConfiguration.Standalone);
        roslynWorkspaceWrapper.CurrentSolution.Returns(v2Solution); // simulate solution closed and opened, so this is a different version now
        testSubject.OnSolutionChanged(solutionName, connectedModeConfiguration);
        
        v2Solution.Received().AddAnalyzerReferences(connectedAnalyzers);
        roslynWorkspaceWrapper.Received().TryApplyChanges(v3Solution);
    }
    
    [TestMethod]
    public void OnSolutionChanged_DifferentSolutionOpened_RegistersAnalyzers()
    {
        var originalSolution = Substitute.For<IRoslynSolutionWrapper>();
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v2Solution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpStandaloneSolution(originalSolution, "original solution");
        EnableDefaultEmbeddedAnalyzers();

        connectedModeRoslynAnalyzerProvider.GetOrNull(connectedModeConfiguration.Project.ServerConnection).Returns(connectedAnalyzers);
        SetUpCurrentSolutionSequence(v1Solution); // different solution
        SetUpAnalyzerAddition(v1Solution, v2Solution, connectedAnalyzers);
        
        testSubject.OnSolutionChanged("different solution", connectedModeConfiguration);

        Received.InOrder(() =>
        {
            v1Solution.AddAnalyzerReferences(connectedAnalyzers);
            roslynWorkspaceWrapper.TryApplyChanges(v2Solution);
        });
    }

    [TestMethod]
    public void HandleConnectedModeAnalyzerUpdate_Standalone_Ignored()
    {
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpStandaloneSolution(v1Solution, "solution");
        EnableDefaultEmbeddedAnalyzers();
        
        testSubject.HandleConnectedModeAnalyzerUpdate(null, new AnalyzerUpdatedForConnectionEventArgs(new ServerConnection.SonarCloud("someorg"), connectedAnalyzers));

        v1Solution.DidNotReceiveWithAnyArgs().AddAnalyzerReferences(default);
        v1Solution.DidNotReceiveWithAnyArgs().RemoveAnalyzerReferences(default);
    }
    
    [TestMethod]
    public void HandleConnectedModeAnalyzerUpdate_Connected_DifferentConnection_Ignored()
    {
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpBoundSolution(v1Solution, "solution");
        EnableDefaultEmbeddedAnalyzers();
        
        testSubject.HandleConnectedModeAnalyzerUpdate(null, new AnalyzerUpdatedForConnectionEventArgs(new ServerConnection.SonarCloud("some other org"), connectedAnalyzers));

        v1Solution.DidNotReceiveWithAnyArgs().AddAnalyzerReferences(default);
        v1Solution.DidNotReceiveWithAnyArgs().RemoveAnalyzerReferences(default);
    }
    
    [TestMethod]
    public void HandleConnectedModeAnalyzerUpdate_Connected_NewAnalyzerSet()
    {
        var connectionWithSameId = new ServerConnection.SonarCloud(((ServerConnection.SonarCloud)connectedModeConfiguration.Project.ServerConnection).OrganizationKey);
        var differentConnectedAnalyzers = ImmutableArray.Create(new AnalyzerFileReference(@"C:\path\connected2", Substitute.For<IAnalyzerAssemblyLoader>()));
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v2Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v3Solution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpBoundSolution(v1Solution, "solution");
        SetUpCurrentSolutionSequence(v1Solution, v2Solution);
        embeddedRoslynAnalyzerProvider.Get().Returns(differentConnectedAnalyzers);
        SetUpAnalyzerRemoval(v1Solution, v2Solution, connectedAnalyzers);
        SetUpAnalyzerAddition(v2Solution, v3Solution, differentConnectedAnalyzers);

        testSubject.HandleConnectedModeAnalyzerUpdate(null, new AnalyzerUpdatedForConnectionEventArgs(connectionWithSameId, connectedAnalyzers));

        Received.InOrder(() =>
        {
            connectedModeRoslynAnalyzerProvider.GetOrNull(connectionWithSameId);
            analyzerComparer.Equals(connectedAnalyzers, differentConnectedAnalyzers);
            v1Solution.RemoveAnalyzerReferences(connectedAnalyzers);
            roslynWorkspaceWrapper.TryApplyChanges(v2Solution);
            v2Solution.AddAnalyzerReferences(differentConnectedAnalyzers);
            roslynWorkspaceWrapper.TryApplyChanges(v3Solution);
        });
    }
    
    [TestMethod]
    public void HandleConnectedModeAnalyzerUpdate_Connected_AnalyzersDidNotChange_DoesNotReRegisterConnected()
    {
        var connectionWithSameId = new ServerConnection.SonarCloud(((ServerConnection.SonarCloud)connectedModeConfiguration.Project.ServerConnection).OrganizationKey);
        var sameSetOfConnectedAnalyzers = ImmutableArray.Create(new AnalyzerFileReference(@"C:\path\connected", Substitute.For<IAnalyzerAssemblyLoader>()));
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpBoundSolution(v1Solution, "solution");
        SetUpCurrentSolutionSequence(v1Solution);
        connectedModeRoslynAnalyzerProvider.GetOrNull(connectionWithSameId).Returns(sameSetOfConnectedAnalyzers);
        analyzerComparer.Equals(connectedAnalyzers, sameSetOfConnectedAnalyzers).Returns(true);

        testSubject.HandleConnectedModeAnalyzerUpdate(null, new AnalyzerUpdatedForConnectionEventArgs(connectionWithSameId, sameSetOfConnectedAnalyzers));

        Received.InOrder(() =>
        {
            connectedModeRoslynAnalyzerProvider.GetOrNull(connectionWithSameId);
            analyzerComparer.Equals(connectedAnalyzers, sameSetOfConnectedAnalyzers);
        });
        embeddedRoslynAnalyzerProvider.DidNotReceiveWithAnyArgs().Get();
        v1Solution.ReceivedCalls().Should().BeEmpty();
        roslynWorkspaceWrapper.DidNotReceiveWithAnyArgs().TryApplyChanges(default);
    }
    
    [TestMethod]
    public void Dispose_UnsubscribesToConnectedModeAnalyzerProviderEvent()
    {
        testSubject.Dispose();

        connectedModeRoslynAnalyzerProvider.Received().AnalyzerUpdatedForConnection -= Arg.Any<EventHandler<AnalyzerUpdatedForConnectionEventArgs>>();
    }
    
    [TestMethod]
    public void OnSolutionChanged_Disposed_Throws()
    {
        var act = () => testSubject.OnSolutionChanged("solution", BindingConfiguration.Standalone);
        testSubject.Dispose();
        
        act.Should().Throw<ObjectDisposedException>();
    }

    [TestMethod]
    public void HandleConnectedModeAnalyzerUpdate_Disposed_Throws()
    {
        var act = () => testSubject.HandleConnectedModeAnalyzerUpdate(null, new AnalyzerUpdatedForConnectionEventArgs(connectedModeConfiguration.Project.ServerConnection, embeddedAnalyzers));
        testSubject.Dispose();
        
        act.Should().Throw<ObjectDisposedException>();
    }
    
    private void SetUpCurrentSolutionSequence(IRoslynSolutionWrapper solution, params IRoslynSolutionWrapper[] solutions)
    {
        roslynWorkspaceWrapper.CurrentSolution.Returns(solution, solutions);
    }
    
    private void SetUpAnalyzerAddition(IRoslynSolutionWrapper originalSolution,
        IRoslynSolutionWrapper resultingSolution,
        ImmutableArray<AnalyzerFileReference> analyzers)
    {
        originalSolution.AddAnalyzerReferences(analyzers).Returns(resultingSolution);
        roslynWorkspaceWrapper.TryApplyChanges(resultingSolution).Returns(true);
    }
    
    private void SetUpAnalyzerRemoval(IRoslynSolutionWrapper originalSolution,
        IRoslynSolutionWrapper resultingSolution,
        ImmutableArray<AnalyzerFileReference> analyzers)
    {
        originalSolution.RemoveAnalyzerReferences(analyzers).Returns(resultingSolution);
        roslynWorkspaceWrapper.TryApplyChanges(resultingSolution).Returns(true);
    }

    private void EnableDefaultEmbeddedAnalyzers()
    {
        embeddedRoslynAnalyzerProvider.Get().Returns(embeddedAnalyzers);
    }

    private void SetUpStandaloneSolution(IRoslynSolutionWrapper solution, string solutionName)
    {
        EnableDefaultEmbeddedAnalyzers();
        SimulateSolutionSet(solution, solutionName, BindingConfiguration.Standalone, embeddedAnalyzers);
    }
    
    private void SetUpBoundSolution(IRoslynSolutionWrapper solution, string solutionName)
    {
        connectedModeRoslynAnalyzerProvider.GetOrNull(default).ReturnsForAnyArgs(connectedAnalyzers);
        SimulateSolutionSet(solution, solutionName, connectedModeConfiguration, connectedAnalyzers);
    }

    private void SimulateSolutionSet(IRoslynSolutionWrapper resultingSolution, string solutionName, BindingConfiguration bindingConfiguration, ImmutableArray<AnalyzerFileReference> analyzers)
    {
        var sourceSolution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpAnalyzerAddition(sourceSolution, resultingSolution, analyzers);
        roslynWorkspaceWrapper.CurrentSolution.Returns(sourceSolution);
        testSubject.OnSolutionChanged(solutionName, bindingConfiguration);
        ClearSubstitutes();
        SetUpCurrentSolutionSequence(resultingSolution);
    }

    private void ClearSubstitutes()
    {
        embeddedRoslynAnalyzerProvider.ClearSubstitute();
        connectedModeRoslynAnalyzerProvider.ClearSubstitute();
        analyzerComparer.ClearSubstitute();
        roslynWorkspaceWrapper.ClearSubstitute();
    }
}
