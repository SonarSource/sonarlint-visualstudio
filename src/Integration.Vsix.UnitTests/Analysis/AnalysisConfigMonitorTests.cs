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
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.CSharpVB.StandaloneMode;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.SLCore.Analysis;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis;

[TestClass]
public class AnalysisConfigMonitorTests
{
    private TestLogger logger;
    private IStandaloneRoslynSettingsUpdater roslynSettingsUpdater;
    private ISLCoreRuleSettingsUpdater slCoreRuleSettingsUpdater;
    private IThreadHandling threadHandling;
    private IUserSettingsProvider userSettingsProviderMock;
    private IInitializationProcessorFactory initializationProcessorFactory;
    private MockableInitializationProcessor createdInitializationProcessor;

    [TestInitialize]
    public void TestInitialize()
    {
        userSettingsProviderMock = Substitute.For<IUserSettingsProvider>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        slCoreRuleSettingsUpdater = Substitute.For<ISLCoreRuleSettingsUpdater>();
        roslynSettingsUpdater = Substitute.For<IStandaloneRoslynSettingsUpdater>();
        logger = new TestLogger();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<AnalysisConfigMonitor, IAnalysisConfigMonitor>(
            MefTestHelpers.CreateExport<IUserSettingsProvider>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ISLCoreRuleSettingsUpdater>(),
            MefTestHelpers.CreateExport<IStandaloneRoslynSettingsUpdater>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<AnalysisConfigMonitor>();

    [TestMethod]
    public void Ctor_UpdatesRoslynSettings()
    {
        var dependencies = new[] { userSettingsProviderMock };
        var userSettings = new UserSettings(new AnalysisSettings(), "any");
        userSettingsProviderMock.UserSettings.Returns(userSettings);

        _ = CreateAndInitializeTestSubject();

        Received.InOrder(() =>
        {
            initializationProcessorFactory.Create<AnalysisConfigMonitor>(
                Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.SequenceEqual(dependencies)),
                Arg.Any<Func<IThreadHandling, Task>>());
            createdInitializationProcessor.InitializeAsync();
            _ = userSettingsProviderMock.UserSettings;
            userSettingsProviderMock.SettingsChanged += Arg.Any<EventHandler>();
            roslynSettingsUpdater.Update(userSettings);
            createdInitializationProcessor.InitializeAsync(); // called as part of the CreateAndInitializeTestSubject method
        });
    }

    [TestMethod]
    public void WhenUninitialized_DoesNotReactToSettingsChangedEvents()
    {
        _ = CreateUninitializedTestSubject(out _);

        SimulateUserSettingsChanged();

        _ = userSettingsProviderMock.DidNotReceiveWithAnyArgs().UserSettings;
    }

    [TestMethod]
    public void WhenUserSettingsChange_UpdatesRoslynAndSlCoreSettings()
    {
        _ = CreateAndInitializeTestSubject();
        roslynSettingsUpdater.ClearReceivedCalls();
        threadHandling.ClearReceivedCalls();

        var userSettings = SimulateUserSettingsChanged();

        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            roslynSettingsUpdater.Update(userSettings);
            slCoreRuleSettingsUpdater.UpdateStandaloneRulesConfiguration();
        });
        logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_UserSettingsChanged);
    }

    [TestMethod]
    public void WhenDisposed_EventsAreIgnored()
    {
        var testSubject = CreateAndInitializeTestSubject();
        roslynSettingsUpdater.ClearReceivedCalls();

        // Act
        testSubject.Dispose();

        // Raise events and check they are ignored
        SimulateUserSettingsChanged();
        roslynSettingsUpdater.DidNotReceiveWithAnyArgs().Update(default);
        slCoreRuleSettingsUpdater.DidNotReceiveWithAnyArgs().UpdateStandaloneRulesConfiguration();
    }

    [TestMethod]
    public void WhenDisposed_InitializationDoesNothing()
    {
        var testSubject = CreateUninitializedTestSubject(out var barrier);
        testSubject.Dispose();

        barrier.SetResult(1);

        userSettingsProviderMock.DidNotReceiveWithAnyArgs().SettingsChanged += Arg.Any<EventHandler>();
    }

    private UserSettings SimulateUserSettingsChanged()
    {
        var userSettings = new UserSettings(new AnalysisSettings(), "any");
        userSettingsProviderMock.UserSettings.Returns(userSettings);
        userSettingsProviderMock.SettingsChanged += Raise.EventWith(null, EventArgs.Empty);
        return userSettings;
    }

    private void AssertSwitchedToBackgroundThread() => threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());

    private AnalysisConfigMonitor CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
    {
        var tcs = barrier = new TaskCompletionSource<byte>();
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<AnalysisConfigMonitor>(threadHandling, logger, processor =>
        {
            MockableInitializationProcessor.ConfigureWithWait(processor, tcs);
            createdInitializationProcessor = processor;
        });
        return new AnalysisConfigMonitor(
            userSettingsProviderMock,
            slCoreRuleSettingsUpdater,
            roslynSettingsUpdater,
            logger,
            threadHandling,
            initializationProcessorFactory);
    }

    private AnalysisConfigMonitor CreateAndInitializeTestSubject()
    {
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<AnalysisConfigMonitor>(threadHandling, logger, processor => createdInitializationProcessor = processor);
        var testSubject = new AnalysisConfigMonitor(
            userSettingsProviderMock,
            slCoreRuleSettingsUpdater,
            roslynSettingsUpdater,
            logger,
            threadHandling,
            initializationProcessorFactory);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        return testSubject;
    }
}
