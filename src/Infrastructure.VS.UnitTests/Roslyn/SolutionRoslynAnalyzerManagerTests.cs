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
using NSubstitute.ClearExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests.Roslyn;

[TestClass]
public class SolutionRoslynAnalyzerManagerTests
{
    private static readonly IEqualityComparer<AnalyzerFileReference> DefaultComparer = EqualityComparer<AnalyzerFileReference>.Default;
    private IBasicRoslynAnalyzerProvider basicRoslynAnalyzerProvider;
    private IEnterpriseRoslynAnalyzerProvider enterpriseRoslynAnalyzerProvider;
    private IRoslynWorkspaceWrapper roslynWorkspaceWrapper;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private IActiveSolutionTracker activeSolutionTracker;
    private IAsyncLockFactory asyncLockFactory;
    private IAsyncLock asyncLock;
    private IEqualityComparer<ImmutableArray<AnalyzerFileReference>?> analyzerComparer;
    private TestLogger logger;
    private SolutionRoslynAnalyzerManager testSubject;
    private readonly ImmutableArray<AnalyzerFileReference> embeddedAnalyzers = ImmutableArray.Create(new AnalyzerFileReference(@"C:\path\embedded", Substitute.For<IAnalyzerAssemblyLoader>()));
    private readonly ImmutableArray<AnalyzerFileReference> connectedAnalyzers =
        ImmutableArray.Create(
            new AnalyzerFileReference(@"C:\path\connected1", Substitute.For<IAnalyzerAssemblyLoader>()),
            new AnalyzerFileReference(@"C:\path\connected2", Substitute.For<IAnalyzerAssemblyLoader>()));

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
        var solution = Substitute.For<IRoslynSolutionWrapper>();
        SetUpAnalyzerUpdate([], embeddedAnalyzers, solution);

        await testSubject.OnSolutionStateChangedAsync("solution");

        roslynWorkspaceWrapper.Received().TryApplyChangesAsync(Arg.Is<IAnalyzerChange>(x => x.AnalyzersToAdd.SequenceEqual(embeddedAnalyzers, DefaultComparer) && x.AnalyzersToRemove.Length == 0));
        AssertNoErrorsInLogs();
    }


    [TestMethod]
    public async Task OnSolutionStateChangedAsync_StandaloneSolution_UpdateFails_Logs()
    {
        basicRoslynAnalyzerProvider.GetBasicAsync()
            .Returns(embeddedAnalyzers);
        IRoslynSolutionWrapper failedUpdate = null;
        SetUpAnalyzerUpdate([], embeddedAnalyzers, failedUpdate);

        await testSubject.OnSolutionStateChangedAsync("solution");

        roslynWorkspaceWrapper.Received().TryApplyChangesAsync(Arg.Is<IAnalyzerChange>(x => x.AnalyzersToAdd.SequenceEqual(embeddedAnalyzers, DefaultComparer) && x.AnalyzersToRemove.Length == 0));
        AssertUpdateFailedAndLogged();
    }

    [TestMethod]
    public async Task OnSolutionStateChangedAsync_StandaloneSolution_BindingSet_RemovesStandaloneAndAppliesConnectedAnalyzer()
    {
        const string solutionName = "solution";
        await SetUpStandaloneSolution(solutionName);
        analyzerComparer.Equals(embeddedAnalyzers, connectedAnalyzers).Returns(false);
        enterpriseRoslynAnalyzerProvider.GetEnterpriseOrNullAsync(solutionName).Returns(connectedAnalyzers);
        SetUpAnalyzerUpdate(embeddedAnalyzers, connectedAnalyzers, Substitute.For<IRoslynSolutionWrapper>());

        await testSubject.OnSolutionStateChangedAsync(solutionName);

        analyzerComparer.Received().Equals(embeddedAnalyzers, connectedAnalyzers);
        roslynWorkspaceWrapper.Received().TryApplyChangesAsync(Arg.Is<IAnalyzerChange>(x =>
            x.AnalyzersToAdd.SequenceEqual(connectedAnalyzers, DefaultComparer) && x.AnalyzersToRemove.SequenceEqual(embeddedAnalyzers, DefaultComparer)));
        basicRoslynAnalyzerProvider.DidNotReceiveWithAnyArgs().GetBasicAsync();
        AssertNoErrorsInLogs();
    }

    [TestMethod]
    public async Task OnSolutionStateChangedAsync_StandaloneSolution_BindingSet_NoConnectedAnalyzer_DoesNotReRegisterEmbedded()
    {
        const string solutionName = "solution";
        await SetUpStandaloneSolution(solutionName);
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
        roslynWorkspaceWrapper.DidNotReceiveWithAnyArgs().TryApplyChangesAsync(default);
        AssertNoErrorsInLogs();
    }

    [TestMethod]
    public async Task OnSolutionStateChangedAsync_SolutionClosedAndReopened_RegistersAnalyzersAgain()
    {
        const string solutionName = "solution";
        await SetUpStandaloneSolution(solutionName);
        EnableDefaultEmbeddedAnalyzers();
        SetUpAnalyzerUpdate(embeddedAnalyzers, [], Substitute.For<IRoslynSolutionWrapper>());
        SetUpAnalyzerUpdate([], embeddedAnalyzers, Substitute.For<IRoslynSolutionWrapper>());

        await testSubject.OnSolutionStateChangedAsync(null);
        await testSubject.OnSolutionStateChangedAsync(solutionName);

        Received.InOrder(() =>
        {
            roslynWorkspaceWrapper.TryApplyChangesAsync(Arg.Is<IAnalyzerChange>(x => x.AnalyzersToAdd.Length == 0 && x.AnalyzersToRemove.SequenceEqual(embeddedAnalyzers, DefaultComparer)));
            enterpriseRoslynAnalyzerProvider.GetEnterpriseOrNullAsync(solutionName);
            basicRoslynAnalyzerProvider.GetBasicAsync();
            roslynWorkspaceWrapper.TryApplyChangesAsync(Arg.Is<IAnalyzerChange>(x => x.AnalyzersToAdd.SequenceEqual(embeddedAnalyzers, DefaultComparer) && x.AnalyzersToRemove.Length == 0));
        });
        AssertNoErrorsInLogs();
    }

    [TestMethod]
    public async Task OnSolutionStateChangedAsync_SolutionClosedAndReopenedAsBound_RegistersConnectedAnalyzers()
    {
        const string solutionName = "solution";
        await SetUpStandaloneSolution(solutionName);
        enterpriseRoslynAnalyzerProvider.GetEnterpriseOrNullAsync(solutionName).Returns(connectedAnalyzers);

        SetUpAnalyzerUpdate(embeddedAnalyzers, [], Substitute.For<IRoslynSolutionWrapper>());
        SetUpAnalyzerUpdate([], connectedAnalyzers, Substitute.For<IRoslynSolutionWrapper>());

        await testSubject.OnSolutionStateChangedAsync(null);
        await testSubject.OnSolutionStateChangedAsync(solutionName);

        Received.InOrder(() =>
        {
            roslynWorkspaceWrapper.TryApplyChangesAsync(Arg.Is<IAnalyzerChange>(x => x.AnalyzersToAdd.Length == 0 && x.AnalyzersToRemove.SequenceEqual(embeddedAnalyzers, DefaultComparer)));
            enterpriseRoslynAnalyzerProvider.GetEnterpriseOrNullAsync(solutionName);
            roslynWorkspaceWrapper.TryApplyChangesAsync(Arg.Is<IAnalyzerChange>(x => x.AnalyzersToAdd.SequenceEqual(connectedAnalyzers, DefaultComparer) && x.AnalyzersToRemove.Length == 0));
        });
        AssertNoErrorsInLogs();
    }

    [TestMethod]
    public async Task OnSolutionStateChangedAsync_DifferentSolutionOpened_RegistersAnalyzers()
    {
        await SetUpStandaloneSolution("original solution");
        var differentSolution = "different solution";
        EnableDefaultEmbeddedAnalyzers();

        enterpriseRoslynAnalyzerProvider.GetEnterpriseOrNullAsync(differentSolution).Returns(connectedAnalyzers);
        SetUpAnalyzerUpdate(embeddedAnalyzers, connectedAnalyzers, Substitute.For<IRoslynSolutionWrapper>());

        await testSubject.OnSolutionStateChangedAsync(differentSolution);

        roslynWorkspaceWrapper.TryApplyChangesAsync(Arg.Is<AnalyzerChange>(x =>
            x.AnalyzersToRemove.SequenceEqual(embeddedAnalyzers, DefaultComparer) && x.AnalyzersToAdd.SequenceEqual(connectedAnalyzers, DefaultComparer)));
        AssertNoErrorsInLogs();
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
        await SetUpStandaloneSolution(solutionName);
        SetUpAnalyzerUpdate(embeddedAnalyzers, [], Substitute.For<IRoslynSolutionWrapper>());

        await testSubject.OnSolutionStateChangedAsync(null);

        await enterpriseRoslynAnalyzerProvider.DidNotReceiveWithAnyArgs().GetEnterpriseOrNullAsync(solutionName);
        AssertNoErrorsInLogs();
    }


    [TestMethod]
    public async Task OnSolutionStateChangedAsync_SolutionClosed_FailedToRemove_Logs()
    {
        const string solutionName = "solution";
        await SetUpStandaloneSolution(solutionName);
        SetUpAnalyzerUpdate(embeddedAnalyzers, [], null);

        await testSubject.OnSolutionStateChangedAsync(null);

        await enterpriseRoslynAnalyzerProvider.DidNotReceiveWithAnyArgs().GetEnterpriseOrNullAsync(solutionName);
        AssertRemoveFailedAndLogged();
    }

    [TestMethod]
    public async Task CurrentConfigurationScopeChanged_SetsAnalyzers()
    {
        const string mySolution = "my solution";
        roslynWorkspaceWrapper.TryApplyChangesAsync(default).ReturnsForAnyArgs(Substitute.For<IRoslynSolutionWrapper>());
        enterpriseRoslynAnalyzerProvider.GetEnterpriseOrNullAsync(mySolution).Returns(embeddedAnalyzers);
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(mySolution));

        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);

        await enterpriseRoslynAnalyzerProvider.Received(1).GetEnterpriseOrNullAsync(mySolution);
        roslynWorkspaceWrapper.ReceivedWithAnyArgs(1).TryApplyChangesAsync(default);
        AssertNoErrorsInLogs();
    }

    [TestMethod]
    public async Task ActiveSolutionChanged_SetsAnalyzers()
    {
        const string solutionName = "my solution";
        roslynWorkspaceWrapper.TryApplyChangesAsync(default).ReturnsForAnyArgs(Substitute.For<IRoslynSolutionWrapper>());
        enterpriseRoslynAnalyzerProvider.GetEnterpriseOrNullAsync(solutionName).Returns(embeddedAnalyzers);

        activeSolutionTracker.ActiveSolutionChanged += Raise.Event<EventHandler<ActiveSolutionChangedEventArgs>>(this, new ActiveSolutionChangedEventArgs(true, solutionName));

        await enterpriseRoslynAnalyzerProvider.Received(1).GetEnterpriseOrNullAsync(solutionName);
        roslynWorkspaceWrapper.ReceivedWithAnyArgs(1).TryApplyChangesAsync(default);
        AssertNoErrorsInLogs();
    }

    private void SetUpAnalyzerUpdate(
        IReadOnlyList<AnalyzerFileReference> analyzersToRemove,
        IReadOnlyList<AnalyzerFileReference> analyzersToAdd,
        IRoslynSolutionWrapper resultingSolution)
    {
        analyzersToRemove ??= [];
        analyzersToAdd ??= [];
        roslynWorkspaceWrapper.TryApplyChangesAsync(
                Arg.Is<IAnalyzerChange>(
                    x =>
                        x.AnalyzersToRemove.SequenceEqual(analyzersToRemove, DefaultComparer)
                        && x.AnalyzersToAdd.SequenceEqual(analyzersToAdd, DefaultComparer)))
            .Returns(resultingSolution);
    }

    private void AssertNoErrorsInLogs()
    {
        logger.AssertPartialOutputStringDoesNotExist(Resources.RoslynAnalyzersNotUpdated);
        logger.AssertPartialOutputStringDoesNotExist(Resources.RoslynAnalyzersNotRemoved);
    }
    private void AssertUpdateFailedAndLogged() => logger.AssertPartialOutputStringExists(Resources.RoslynAnalyzersNotUpdated);
    private void AssertRemoveFailedAndLogged() => logger.AssertPartialOutputStringExists(Resources.RoslynAnalyzersNotRemoved);

    private void EnableDefaultEmbeddedAnalyzers()
    {
        basicRoslynAnalyzerProvider.GetBasicAsync().Returns(embeddedAnalyzers);
    }

    private async Task SetUpStandaloneSolution(string solutionName)
    {
        EnableDefaultEmbeddedAnalyzers();
        await SimulateSolutionSet(Substitute.For<IRoslynSolutionWrapper>(), solutionName);
    }

    private async Task SimulateSolutionSet(IRoslynSolutionWrapper resultingSolution, string solutionName)
    {
        roslynWorkspaceWrapper.TryApplyChangesAsync(Arg.Any<IAnalyzerChange>()).Returns(resultingSolution);
        await testSubject.OnSolutionStateChangedAsync(solutionName);
        ClearSubstitutes();
    }

    private void ClearSubstitutes()
    {
        basicRoslynAnalyzerProvider.ClearSubstitute();
        enterpriseRoslynAnalyzerProvider.ClearSubstitute();
        analyzerComparer.ClearSubstitute();
        roslynWorkspaceWrapper.ClearSubstitute();
    }
}
