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

using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.SLCore.Analysis;
using static SonarLint.VisualStudio.TestInfrastructure.NoOpThreadHandler;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis.UnitTests
{
    [TestClass]
    public class AnalysisConfigMonitorTests
    {
        private IAnalysisRequester analysisRequesterMock;
        private IUserSettingsProvider userSettingsUpdaterMock;
        private IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private INotifyQualityProfilesChanged notifyQPsUpdated;
        private IThreadHandling threadHandling;
        private ISLCoreRuleSettingsUpdater slCoreRuleSettingsUpdater;
        private TestLogger logger;
        private AnalysisConfigMonitor testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            analysisRequesterMock = Substitute.For<IAnalysisRequester>();
            userSettingsUpdaterMock = Substitute.For<IUserSettingsProvider>();
            activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
            notifyQPsUpdated = Substitute.For<INotifyQualityProfilesChanged>();
            threadHandling = Substitute.For<IThreadHandling>();
            slCoreRuleSettingsUpdater = Substitute.For<ISLCoreRuleSettingsUpdater>();

            threadHandling.SwitchToBackgroundThread().Returns(new NoOpAwaitable());

            logger = new TestLogger();

            testSubject = new AnalysisConfigMonitor(analysisRequesterMock, 
                userSettingsUpdaterMock, 
                activeSolutionBoundTracker, 
                notifyQPsUpdated, 
                logger, 
                threadHandling, 
                slCoreRuleSettingsUpdater);
        }


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
            SimulateUserSettingsChanged();

            Received.InOrder(() =>
            {
                slCoreRuleSettingsUpdater.UpdateStandaloneRulesConfiguration();
                analysisRequesterMock.RequestAnalysis(null, Array.Empty<string>());
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
        public void WhenQualityProfilesChanged_AnalysisIsRequested()
        {
            SimulateQualityProfilesChanged();

            // Should re-analyse
            AssertAnalysisIsRequested();
            AssertSwitchedToBackgroundThread();
            logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_QualityProfilesChanged);
        }

        [TestMethod]
        public void WhenDisposed_EventsAreIgnored()
        {
            // Act
            testSubject.Dispose();

            // Raise events and check they are ignored
            SimulateUserSettingsChanged();
            SimulateBindingChanged();
            SimulateQualityProfilesChanged();
            AssertAnalysisIsNotRequested();
        }

        private void SimulateUserSettingsChanged()
            => userSettingsUpdaterMock.SettingsChanged += Raise.EventWith(null, EventArgs.Empty);

        private void SimulateBindingChanged(BindingConfiguration config = null)
            => activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(null, new ActiveSolutionBindingEventArgs(config));

        private void SimulateQualityProfilesChanged()
            => notifyQPsUpdated.QualityProfilesChanged += Raise.EventWith(null, EventArgs.Empty);

        private void AssertAnalysisIsRequested()
            => analysisRequesterMock.Received(1).RequestAnalysis(null, Array.Empty<string>());

        private void AssertAnalysisIsNotRequested()
            => analysisRequesterMock.ReceivedCalls().Count().Should().Be(0);

        private void AssertSwitchedToBackgroundThread()
            => threadHandling.Received(1).SwitchToBackgroundThread();
    }
}
