﻿/*
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
using NSubstitute.ClearExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests.Roslyn;

[TestClass]
public class SolutionRoslynAnalyzerManagerTests
{
    private IBasicRoslynAnalyzerProvider basicRoslynAnalyzerProvider;
    private IEnterpriseRoslynAnalyzerProvider enterpriseRoslynAnalyzerProvider;
    private IRoslynWorkspaceWrapper roslynWorkspaceWrapper;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private IActiveSolutionTracker activeSolutionTracker;
    private IAsyncLockFactory asyncLockFactory;
    private IAsyncLock asyncLock;
    private IEqualityComparer<ImmutableArray<AnalyzerFileReference>?> analyzerComparer;
    private ILogger logger;
    private SolutionRoslynAnalyzerManager testSubject;
    private readonly ImmutableArray<AnalyzerFileReference> embeddedAnalyzers = ImmutableArray.Create(new AnalyzerFileReference(@"C:\path\embedded", Substitute.For<IAnalyzerAssemblyLoader>()));
    private readonly ImmutableArray<AnalyzerFileReference> connectedAnalyzers = ImmutableArray.Create(new AnalyzerFileReference(@"C:\path\connected", Substitute.For<IAnalyzerAssemblyLoader>()));

    [TestInitialize]
    public void TestInitialize()
    {
        logger = new TestLogger();
        basicRoslynAnalyzerProvider = Substitute.For<IBasicRoslynAnalyzerProvider>();
        enterpriseRoslynAnalyzerProvider = Substitute.For<IEnterpriseRoslynAnalyzerProvider>();
        roslynWorkspaceWrapper = Substitute.For<IRoslynWorkspaceWrapper>();
        analyzerComparer = Substitute.For<IEqualityComparer<ImmutableArray<AnalyzerFileReference>?>>();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        activeSolutionTracker = Substitute.For<IActiveSolutionTracker>();
        asyncLockFactory = Substitute.For<IAsyncLockFactory>();
        asyncLock = Substitute.For<IAsyncLock>();
        asyncLockFactory.Create().Returns(asyncLock);

        testSubject = new SolutionRoslynAnalyzerManager(
            basicRoslynAnalyzerProvider,
            enterpriseRoslynAnalyzerProvider,
            roslynWorkspaceWrapper,
            analyzerComparer,
            activeConfigScopeTracker,
            activeSolutionTracker,
            asyncLockFactory,
            logger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SolutionRoslynAnalyzerManager, ISolutionRoslynAnalyzerManager>(
            MefTestHelpers.CreateExport<IBasicRoslynAnalyzerProvider>(),
            MefTestHelpers.CreateExport<IEnterpriseRoslynAnalyzerProvider>(),
            MefTestHelpers.CreateExport<IRoslynWorkspaceWrapper>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IActiveSolutionTracker>(),
            MefTestHelpers.CreateExport<IAsyncLockFactory>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SolutionRoslynAnalyzerManager>();
    }

    [TestMethod]
    public void Ctor_SubscribesToEvents()
    {
        new SolutionRoslynAnalyzerManager(
            basicRoslynAnalyzerProvider,
            enterpriseRoslynAnalyzerProvider,
            roslynWorkspaceWrapper,
            analyzerComparer,
            activeConfigScopeTracker,
            activeSolutionTracker,
            asyncLockFactory,
            logger);

        // todo add more tests for the events
        activeConfigScopeTracker.Received().CurrentConfigurationScopeChanged += Arg.Any<EventHandler>();
        activeSolutionTracker.Received().ActiveSolutionChanged += Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
    }

    [TestMethod]
    public async Task OnSolutionStateChangedAsync_NoOpenSolution_DoesNothing()
    {
        await testSubject.OnSolutionStateChangedAsync(null);

        roslynWorkspaceWrapper.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public async Task OnSolutionStateChangedAsync_AcquiresLock()
    {
        await testSubject.OnSolutionStateChangedAsync(null);

        await asyncLock.Received(1).AcquireAsync();
    }

    [TestMethod]
    public async Task OnSolutionStateChangedAsync_StandaloneSolution_AppliesEmbeddedAnalyzer()
    {
        basicRoslynAnalyzerProvider.GetBasicAsync()
            .Returns(embeddedAnalyzers);
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v2Solution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpCurrentSolutionSequence(v1Solution);
        SetUpAnalyzerAddition(v1Solution, v2Solution, embeddedAnalyzers);

        await testSubject.OnSolutionStateChangedAsync("solution");

        roslynWorkspaceWrapper.Received().TryApplyChanges(v2Solution);
    }

    [TestMethod]
    public async Task OnSolutionStateChangedAsync_StandaloneSolution_BindingSet_RemovesStandaloneAndAppliesConnectedAnalyzer()
    {
        const string solutionName = "solution";
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v2Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v3Solution = Substitute.For<IRoslynSolutionWrapper>();
        await SetUpStandaloneSolution(v1Solution, solutionName);

        analyzerComparer.Equals(embeddedAnalyzers, connectedAnalyzers).Returns(false);
        enterpriseRoslynAnalyzerProvider.GetEnterpriseOrNullAsync(solutionName).Returns(connectedAnalyzers);
        roslynWorkspaceWrapper.CurrentSolution.Returns(v1Solution, v2Solution);
        SetUpAnalyzerRemoval(v1Solution, v2Solution, embeddedAnalyzers);
        SetUpAnalyzerAddition(v2Solution, v3Solution, connectedAnalyzers);

        await testSubject.OnSolutionStateChangedAsync(solutionName);

        Received.InOrder(() =>
        {
            analyzerComparer.Equals(embeddedAnalyzers, connectedAnalyzers);
            v1Solution.RemoveAnalyzerReferences(embeddedAnalyzers);
            roslynWorkspaceWrapper.TryApplyChanges(v2Solution);
            v2Solution.AddAnalyzerReferences(connectedAnalyzers);
            roslynWorkspaceWrapper.TryApplyChanges(v3Solution);
        });
        basicRoslynAnalyzerProvider.DidNotReceiveWithAnyArgs().GetBasicAsync();
    }

    [TestMethod]
    public async Task OnSolutionStateChangedAsync_StandaloneSolution_BindingSet_NoConnectedAnalyzer_DoesNotReRegisterEmbedded()
    {
        const string solutionName = "solution";
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        await SetUpStandaloneSolution(v1Solution, solutionName);
        EnableDefaultEmbeddedAnalyzers();
        enterpriseRoslynAnalyzerProvider.GetEnterpriseOrNullAsync(solutionName).Returns((ImmutableArray<AnalyzerFileReference>?)null);
        analyzerComparer.Equals(embeddedAnalyzers, embeddedAnalyzers).Returns(true);

        await testSubject.OnSolutionStateChangedAsync(solutionName);

        Received.InOrder(() =>
        {
            enterpriseRoslynAnalyzerProvider.GetEnterpriseOrNullAsync(solutionName);
            basicRoslynAnalyzerProvider.GetBasicAsync();
            analyzerComparer.Equals(embeddedAnalyzers, embeddedAnalyzers);
        });
        roslynWorkspaceWrapper.DidNotReceiveWithAnyArgs().TryApplyChanges(default);
    }

    [TestMethod]
    public async Task OnSolutionStateChangedAsync_SolutionClosedAndReopened_RegistersAnalyzersAgain()
    {
        const string solutionName = "solution";
        var preCloseSolution = Substitute.For<IRoslynSolutionWrapper>();
        var v2Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v3Solution = Substitute.For<IRoslynSolutionWrapper>();
        await SetUpStandaloneSolution(preCloseSolution, solutionName);
        EnableDefaultEmbeddedAnalyzers();
        SetUpCurrentSolutionSequence(v2Solution);
        SetUpAnalyzerAddition(v2Solution, v3Solution, embeddedAnalyzers);
        SetUpAnalyzerRemoval(v2Solution, v3Solution, embeddedAnalyzers);

        await testSubject.OnSolutionStateChangedAsync(null);
        await testSubject.OnSolutionStateChangedAsync(solutionName);

        v2Solution.Received().AddAnalyzerReferences(embeddedAnalyzers);
        roslynWorkspaceWrapper.Received().TryApplyChanges(v3Solution);
    }

    [TestMethod]
    public async Task OnSolutionStateChangedAsync_SolutionClosedAndReopenedAsBound_RegistersConnectedAnalyzers()
    {
        const string solutionName = "solution";
        var preCloseSolution = Substitute.For<IRoslynSolutionWrapper>();
        var v2Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v3Solution = Substitute.For<IRoslynSolutionWrapper>();
        await SetUpStandaloneSolution(preCloseSolution, solutionName);
        EnableDefaultEmbeddedAnalyzers();
        enterpriseRoslynAnalyzerProvider.GetEnterpriseOrNullAsync(solutionName).Returns(connectedAnalyzers);
        SetUpAnalyzerAddition(v2Solution, v3Solution, connectedAnalyzers);
        SetUpAnalyzerRemoval(preCloseSolution, v2Solution, embeddedAnalyzers);

        await testSubject.OnSolutionStateChangedAsync(null);
        roslynWorkspaceWrapper.CurrentSolution.Returns(v2Solution); // simulate solution closed and opened, so this is a different version now
        await testSubject.OnSolutionStateChangedAsync(solutionName);

        v2Solution.Received().AddAnalyzerReferences(connectedAnalyzers);
        roslynWorkspaceWrapper.Received().TryApplyChanges(v3Solution);
    }

    [TestMethod]
    public async Task OnSolutionStateChangedAsync_DifferentSolutionOpened_RegistersAnalyzers()
    {
        var originalSolution = Substitute.For<IRoslynSolutionWrapper>();
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        var v2Solution = Substitute.For<IRoslynSolutionWrapper>();
        await SetUpStandaloneSolution(originalSolution, "original solution");
        var differentSolution = "different solution";
        EnableDefaultEmbeddedAnalyzers();

        enterpriseRoslynAnalyzerProvider.GetEnterpriseOrNullAsync(differentSolution).Returns(connectedAnalyzers);
        SetUpCurrentSolutionSequence(v1Solution); // different solution
        SetUpAnalyzerAddition(v1Solution, v2Solution, connectedAnalyzers);
        SetUpAnalyzerRemoval(v1Solution, v2Solution, embeddedAnalyzers);

        await testSubject.OnSolutionStateChangedAsync(differentSolution);

        Received.InOrder(() =>
        {
            v1Solution.RemoveAnalyzerReferences(embeddedAnalyzers);
            roslynWorkspaceWrapper.TryApplyChanges(v2Solution);
            v1Solution.AddAnalyzerReferences(connectedAnalyzers);
            roslynWorkspaceWrapper.TryApplyChanges(v2Solution);
        });
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromEvents()
    {
        testSubject.Dispose();

        activeConfigScopeTracker.Received().CurrentConfigurationScopeChanged -= Arg.Any<EventHandler>();
        activeSolutionTracker.Received().ActiveSolutionChanged -= Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
    }

    [TestMethod]
    public void OnSolutionStateChangedAsync_Disposed_Throws()
    {
        var act = async () => await testSubject.OnSolutionStateChangedAsync("solution");
        testSubject.Dispose();

        act.Should().Throw<ObjectDisposedException>();
    }

    [TestMethod]
    public async Task OnSolutionStateChangedAsync_SolutionClosed_RemovesAnalzyers()
    {
        const string solutionName = "solution";
        var v1Solution = Substitute.For<IRoslynSolutionWrapper>();
        await SetUpStandaloneSolution(v1Solution, solutionName);
        SetUpAnalyzerRemoval(v1Solution, Substitute.For<IRoslynSolutionWrapper>(), embeddedAnalyzers);

        await testSubject.OnSolutionStateChangedAsync(null);

        v1Solution.Received(1).RemoveAnalyzerReferences(embeddedAnalyzers);
        await enterpriseRoslynAnalyzerProvider.DidNotReceiveWithAnyArgs().GetEnterpriseOrNullAsync(solutionName);
    }

    [TestMethod]
    public async Task CurrentConfigurationScopeChanged_SetsAnalyzers()
    {
        const string mySolution = "my solution";
        roslynWorkspaceWrapper.TryApplyChanges(Arg.Any<IRoslynSolutionWrapper>()).Returns(true);
        enterpriseRoslynAnalyzerProvider.GetEnterpriseOrNullAsync(mySolution).Returns(embeddedAnalyzers);
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(mySolution));

        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);

        await enterpriseRoslynAnalyzerProvider.Received(1).GetEnterpriseOrNullAsync(mySolution);
        roslynWorkspaceWrapper.Received(1).TryApplyChanges(Arg.Any<IRoslynSolutionWrapper>());
    }

    [TestMethod]
    public async Task ActiveSolutionChanged_SetsAnalyzers()
    {
        const string solutionName = "my solution";
        roslynWorkspaceWrapper.TryApplyChanges(Arg.Any<IRoslynSolutionWrapper>()).Returns(true);
        enterpriseRoslynAnalyzerProvider.GetEnterpriseOrNullAsync(solutionName).Returns(embeddedAnalyzers);

        activeSolutionTracker.ActiveSolutionChanged += Raise.Event<EventHandler<ActiveSolutionChangedEventArgs>>(this, new ActiveSolutionChangedEventArgs(true, solutionName));

        await enterpriseRoslynAnalyzerProvider.Received(1).GetEnterpriseOrNullAsync(solutionName);
        roslynWorkspaceWrapper.Received(1).TryApplyChanges(Arg.Any<IRoslynSolutionWrapper>());
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
        basicRoslynAnalyzerProvider.GetBasicAsync().Returns(embeddedAnalyzers);
    }

    private async Task SetUpStandaloneSolution(IRoslynSolutionWrapper solution, string solutionName)
    {
        EnableDefaultEmbeddedAnalyzers();
        await SimulateSolutionSet(solution, solutionName, embeddedAnalyzers);
    }

    private async Task SimulateSolutionSet(IRoslynSolutionWrapper resultingSolution, string solutionName, ImmutableArray<AnalyzerFileReference> analyzers)
    {
        var sourceSolution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpAnalyzerAddition(sourceSolution, resultingSolution, analyzers);
        roslynWorkspaceWrapper.CurrentSolution.Returns(sourceSolution);
        await testSubject.OnSolutionStateChangedAsync(solutionName);
        ClearSubstitutes();
        SetUpCurrentSolutionSequence(resultingSolution);
    }

    private void ClearSubstitutes()
    {
        basicRoslynAnalyzerProvider.ClearSubstitute();
        enterpriseRoslynAnalyzerProvider.ClearSubstitute();
        analyzerComparer.ClearSubstitute();
        roslynWorkspaceWrapper.ClearSubstitute();
    }
}
