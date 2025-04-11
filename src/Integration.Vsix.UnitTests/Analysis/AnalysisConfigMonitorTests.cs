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
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Integration.CSharpVB.StandaloneMode;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.SLCore.Analysis;
using static SonarLint.VisualStudio.TestInfrastructure.NoOpThreadHandler;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis;

[TestClass]
public class AnalysisConfigMonitorTests
{
    private IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private IAnalysisRequester analysisRequesterMock;
    private TestLogger logger;
    private IStandaloneRoslynSettingsUpdater roslynSettingsUpdater;
    private ISLCoreRuleSettingsUpdater slCoreRuleSettingsUpdater;
    private AnalysisConfigMonitor testSubject;
    private IThreadHandling threadHandling;
    private IUserSettingsProvider userSettingsUpdaterMock;

    [TestInitialize]
    public void TestInitialize()
    {
        analysisRequesterMock = Substitute.For<IAnalysisRequester>();
        userSettingsUpdaterMock = Substitute.For<IUserSettingsProvider>();
        activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        threadHandling = Substitute.For<IThreadHandling>();
        slCoreRuleSettingsUpdater = Substitute.For<ISLCoreRuleSettingsUpdater>();
        roslynSettingsUpdater = Substitute.For<IStandaloneRoslynSettingsUpdater>();

        threadHandling.SwitchToBackgroundThread().Returns(new NoOpAwaitable());

        logger = new TestLogger();

        testSubject = new AnalysisConfigMonitor(
            analysisRequesterMock,
            userSettingsUpdaterMock,
            slCoreRuleSettingsUpdater,
            roslynSettingsUpdater,
            activeSolutionBoundTracker,
            logger,
            threadHandling);
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
            MefTestHelpers.CreateExport<IStandaloneRoslynSettingsUpdater>());

    [TestMethod]
    public void WhenUserSettingsChange_AnalysisIsRequested()
    {
        SimulateUserSettingsChanged();

        // Should re-analyse
        AssertAnalysisIsRequested();
        AssertSwitchedToBackgroundThread();
        logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_UserSettingsChanged);
    }

    [TestMethod]
    public void WhenUserSettingsChange_UpdatesSlCoreSettingsBeforeTriggeringAnalysis()
    {
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
        SimulateBindingChanged();

        // Should re-analyse
        AssertAnalysisIsRequested();
        AssertSwitchedToBackgroundThread();
        logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_BindingChanged);
    }

    [TestMethod]
    public void WhenDisposed_EventsAreIgnored()
    {
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
}
