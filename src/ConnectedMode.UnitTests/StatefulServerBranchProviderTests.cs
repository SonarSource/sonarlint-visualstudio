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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Branch;
using SonarLint.VisualStudio.SLCore.Service.Branch;
using SonarLint.VisualStudio.SLCore.State;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests;

[TestClass]
public class SLCoreGitChangeNotifierTests
{
    private NoOpThreadHandler threadHandling;
    private ISLCoreServiceProvider slCoreServiceProvider;
    private TestLogger logger;
    private SlCoreGitChangeNotifier testSubject;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private ISonarProjectBranchSlCoreService sonarProjectBranchSlCoreService;
    private IBoundSolutionGitMonitor gitMonitor;
    private IInitializationProcessorFactory initializationProcessorFactory;

    [TestInitialize]
    public void TestInitialize()
    {
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        logger = Substitute.ForPartsOf<TestLogger>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        gitMonitor = Substitute.For<IBoundSolutionGitMonitor>();

        MockSonarProjectBranchSlCoreService();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SlCoreGitChangeNotifier, ISlCoreGitChangeNotifier>(
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IBoundSolutionGitMonitor>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SlCoreGitChangeNotifier>();

    [TestMethod]
    public void WhenInitialized_EventHandlersAreRegistered()
    {
        CreateAndInitializeTestSubject();

        var initializationDependencies = new IRequireInitialization[] { gitMonitor };
        initializationProcessorFactory.Received(1).Create<SlCoreGitChangeNotifier>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.SequenceEqual(initializationDependencies)), Arg.Any<Func<IThreadHandling, Task>>());

        Received.InOrder(() =>
        {
            threadHandling.RunOnUIThreadAsync(Arg.Any<Action>());
            gitMonitor.HeadChanged += Arg.Any<EventHandler>();
            activeConfigScopeTracker.CurrentConfigurationScopeChanged += Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
        });
    }

    [TestMethod]
    public void OnHeadChanged_NotifiesSlCoreAboutBranchChange()
    {
        CreateAndInitializeTestSubject();

        const string expectedConfigScopeId = "expected-id";
        var configScope = new Core.ConfigurationScope.ConfigurationScope(expectedConfigScopeId);
        activeConfigScopeTracker.Current.Returns(configScope);
        MockSonarProjectBranchSlCoreService();

        gitMonitor.HeadChanged += Raise.EventWith(gitMonitor, EventArgs.Empty);

        sonarProjectBranchSlCoreService.Received(1)
            .DidVcsRepositoryChange(Arg.Is<DidVcsRepositoryChangeParams>(p => p.configurationScopeId == expectedConfigScopeId));
    }

    [TestMethod]
    public void OnHeadChanged_WhenConfigScopeIsNull_DoesNotNotifySlCore()
    {
        CreateAndInitializeTestSubject();

        activeConfigScopeTracker.Current.Returns((Core.ConfigurationScope.ConfigurationScope)null);
        MockSonarProjectBranchSlCoreService();

        gitMonitor.HeadChanged += Raise.EventWith(gitMonitor, EventArgs.Empty);

        sonarProjectBranchSlCoreService.DidNotReceive()
            .DidVcsRepositoryChange(Arg.Any<DidVcsRepositoryChangeParams>());
    }

    [TestMethod]
    public void OnHeadChanged_WhenServiceProviderFails_LogsError()
    {
        CreateAndInitializeTestSubject();

        const string expectedConfigScopeId = "expected-id";
        var configScope = new Core.ConfigurationScope.ConfigurationScope(expectedConfigScopeId);
        activeConfigScopeTracker.Current.Returns(configScope);
        MockSonarProjectBranchSlCoreService(wasFound: false);

        gitMonitor.HeadChanged += Raise.EventWith(gitMonitor, EventArgs.Empty);

        logger.AssertPartialOutputStringExists(SLCoreStrings.ServiceProviderNotInitialized);
        sonarProjectBranchSlCoreService.DidNotReceive()
            .DidVcsRepositoryChange(Arg.Any<DidVcsRepositoryChangeParams>());
    }

    [TestMethod]
    public void OnConfigScopeChanged_WhenDefinitionChanges_RefreshesGitMonitor()
    {
        CreateAndInitializeTestSubject();

        var args = new ConfigurationScopeChangedEventArgs(true);
        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(activeConfigScopeTracker, args);

        gitMonitor.Received(1).Refresh();
    }

    [TestMethod]
    public void OnConfigScopeChanged_WhenDefinitionDoesNotChange_DoesNotRefreshGitMonitor()
    {
        CreateAndInitializeTestSubject();

        var args = new ConfigurationScopeChangedEventArgs(false);
        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(activeConfigScopeTracker, args);

        gitMonitor.DidNotReceive().Refresh();
    }

    [TestMethod]
    public void Dispose_WhenInitialized_UnhooksEventHandlers()
    {
        CreateAndInitializeTestSubject();

        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        gitMonitor.ReceivedWithAnyArgs(1).HeadChanged -= default;
        activeConfigScopeTracker.ReceivedWithAnyArgs(1).CurrentConfigurationScopeChanged -= default;
    }

    [TestMethod]
    public void Dispose_WhenNotInitialized_DoesNotExecute()
    {
        CreateUninitializedTestSubject(out var barrier);

        CheckDisposed();

        barrier.SetResult(1);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        testSubject.InitializationProcessor.IsFinalized.Should().BeTrue();
        CheckDisposed();

        void CheckDisposed()
        {
            var act = () => testSubject.Dispose();

            act.Should().NotThrow();
            gitMonitor.DidNotReceiveWithAnyArgs().HeadChanged += default;
            gitMonitor.DidNotReceiveWithAnyArgs().HeadChanged -= default;
            activeConfigScopeTracker.DidNotReceiveWithAnyArgs().CurrentConfigurationScopeChanged += default;
            activeConfigScopeTracker.DidNotReceiveWithAnyArgs().CurrentConfigurationScopeChanged -= default;
        }
    }

    private void MockSonarProjectBranchSlCoreService(bool wasFound = true)
    {
        sonarProjectBranchSlCoreService = Substitute.For<ISonarProjectBranchSlCoreService>();
        slCoreServiceProvider.TryGetTransientService(out ISonarProjectBranchSlCoreService _).Returns(x =>
        {
            x[0] = sonarProjectBranchSlCoreService;
            return wasFound;
        });
    }

    private void CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
    {
        var tcs = barrier = new TaskCompletionSource<byte>();
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<SlCoreGitChangeNotifier>(
            threadHandling,
            logger,
            processor => MockableInitializationProcessor.ConfigureWithWait(processor, tcs));

        testSubject = new SlCoreGitChangeNotifier(
            activeConfigScopeTracker,
            slCoreServiceProvider,
            gitMonitor,
            logger,
            threadHandling,
            initializationProcessorFactory);
    }

    private void CreateAndInitializeTestSubject()
    {
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<SlCoreGitChangeNotifier>(threadHandling, logger);

        testSubject = new SlCoreGitChangeNotifier(
            activeConfigScopeTracker,
            slCoreServiceProvider,
            gitMonitor,
            logger,
            threadHandling,
            initializationProcessorFactory);

        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
    }
}
