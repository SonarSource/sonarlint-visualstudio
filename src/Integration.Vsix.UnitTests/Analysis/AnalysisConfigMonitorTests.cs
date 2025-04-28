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
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.CSharpVB.StandaloneMode;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.SLCore.Analysis;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis;

[TestClass]
public class AnalysisConfigMonitorTests
{
    private IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private IAnalysisRequester analysisRequesterMock;
    private TestLogger logger;
    private IStandaloneRoslynSettingsUpdater roslynSettingsUpdater;
    private ISLCoreRuleSettingsUpdater slCoreRuleSettingsUpdater;
    private IThreadHandling threadHandling;
    private IUserSettingsProvider userSettingsUpdaterMock;
    private IInitializationProcessorFactory initializationProcessorFactory;
    private MockableInitializationProcessor createdInitializationProcessor;

    [TestInitialize]
    public void TestInitialize()
    {
        analysisRequesterMock = Substitute.For<IAnalysisRequester>();
        userSettingsUpdaterMock = Substitute.For<IUserSettingsProvider>();
        activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        slCoreRuleSettingsUpdater = Substitute.For<ISLCoreRuleSettingsUpdater>();
        roslynSettingsUpdater = Substitute.For<IStandaloneRoslynSettingsUpdater>();
        logger = new TestLogger();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<AnalysisConfigMonitor, IAnalysisConfigMonitor>(
            MefTestHelpers.CreateExport<IAnalysisRequester>(),
            MefTestHelpers.CreateExport<IUserSettingsProvider>(),
            MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
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
        var dependencies = new[] { activeSolutionBoundTracker };
        var userSettings = new UserSettings(new AnalysisSettings());
        userSettingsUpdaterMock.UserSettings.Returns(userSettings);

        _ = CreateAndInitializeTestSubject();

        Received.InOrder(() =>
        {
            initializationProcessorFactory.Create<AnalysisConfigMonitor>(
                Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.SequenceEqual(dependencies)),
                Arg.Any<Func<IThreadHandling, Task>>());
            createdInitializationProcessor.InitializeAsync();
            _ = userSettingsUpdaterMock.UserSettings;
            roslynSettingsUpdater.Update(userSettings);
            userSettingsUpdaterMock.SettingsChanged += Arg.Any<EventHandler>();
            activeSolutionBoundTracker.SolutionBindingChanged += Arg.Any<EventHandler<ActiveSolutionBindingEventArgs>>();
            createdInitializationProcessor.InitializeAsync(); // called as part of the CreateAndInitializeTestSubject method
        });
    }

    [TestMethod]
    public void WhenUserSettingsChange_AnalysisIsRequested()
    {
        _ = CreateAndInitializeTestSubject();

        SimulateUserSettingsChanged();

        // Should re-analyse
        AssertAnalysisIsRequested();
        AssertSwitchedToBackgroundThread();
        logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_UserSettingsChanged);
    }

    [TestMethod]
    public void WhenUserSettingsChange_UpdatesSlCoreSettingsBeforeTriggeringAnalysis()
    {
        _ = CreateAndInitializeTestSubject();

        var userSettings = SimulateUserSettingsChanged();

        Received.InOrder(() =>
        {
            roslynSettingsUpdater.Update(userSettings);
            slCoreRuleSettingsUpdater.UpdateStandaloneRulesConfiguration();
            analysisRequesterMock.RequestAnalysis(null);
        });
    }

    [TestMethod]
    public void WhenBindingChanges_AnalysisIsRequested()
    {
        _ = CreateAndInitializeTestSubject();

        SimulateBindingChanged();

        // Should re-analyse
        AssertAnalysisIsRequested();
        AssertSwitchedToBackgroundThread();
        logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_BindingChanged);
    }

    [TestMethod]
    public void WhenDisposed_EventsAreIgnored()
    {
        var testSubject = CreateAndInitializeTestSubject();

        // Act
        testSubject.Dispose();

        // Raise events and check they are ignored
        SimulateUserSettingsChanged();
        SimulateBindingChanged();
        AssertAnalysisIsNotRequested();
    }

    private UserSettings SimulateUserSettingsChanged()
    {
        var userSettings = new UserSettings(new AnalysisSettings());
        userSettingsUpdaterMock.UserSettings.Returns(userSettings);
        userSettingsUpdaterMock.SettingsChanged += Raise.EventWith(null, EventArgs.Empty);
        return userSettings;
    }

    private void SimulateBindingChanged(BindingConfiguration config = null) => activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(null, new ActiveSolutionBindingEventArgs(config));

    private void AssertAnalysisIsRequested() => analysisRequesterMock.Received(1).RequestAnalysis(null);

    private void AssertAnalysisIsNotRequested() => analysisRequesterMock.ReceivedCalls().Count().Should().Be(0);

    private void AssertSwitchedToBackgroundThread() => threadHandling.Received(1).SwitchToBackgroundThread();

    private AnalysisConfigMonitor CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
    {
        var tcs = barrier = new TaskCompletionSource<byte>();
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<AnalysisConfigMonitor>(threadHandling, logger, processor =>
        {
            MockableInitializationProcessor.ConfigureWithWait(processor, tcs);
            createdInitializationProcessor = processor;
        });
        return new AnalysisConfigMonitor(
            analysisRequesterMock,
            userSettingsUpdaterMock,
            slCoreRuleSettingsUpdater,
            roslynSettingsUpdater,
            activeSolutionBoundTracker,
            logger,
            threadHandling,
            initializationProcessorFactory);
    }

    private AnalysisConfigMonitor CreateAndInitializeTestSubject()
    {
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<AnalysisConfigMonitor>(threadHandling, logger, processor => createdInitializationProcessor = processor);
        var testSubject = new AnalysisConfigMonitor(
            analysisRequesterMock,
            userSettingsUpdaterMock,
            slCoreRuleSettingsUpdater,
            roslynSettingsUpdater,
            activeSolutionBoundTracker,
            logger,
            threadHandling,
            initializationProcessorFactory);
        testSubject.InitializeAsync().GetAwaiter().GetResult();
        return testSubject;
    }
}
