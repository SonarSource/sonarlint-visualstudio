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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.TestInfrastructure;
using static SonarLint.VisualStudio.TestInfrastructure.NoOpThreadHandler;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis.UnitTests
{
    [TestClass]
    public class AnalysisConfigMonitorTests
    {
        [TestMethod]
        public void WhenUserSettingsChange_AnalysisIsRequested()
        {
            var builder = new TestEnvironmentBuilder();

            builder.SimulateUserSettingsChanged();

            // Should re-analyse
            builder.AssertAnalysisIsRequested();
            builder.AssertSwitchedToBackgroundThread();
            builder.Logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_UserSettingsChanged);
        }

        [TestMethod]
        public void WhenUserSettingsChange_HasSubscribersToConfigChangedEvent_SubscribersNotified()
        {
            var builder = new TestEnvironmentBuilder();
            var eventHandler = new Mock<EventHandler>();
            builder.TestSubject.ConfigChanged += eventHandler.Object;

            builder.SimulateUserSettingsChanged();
            builder.AssertSwitchedToBackgroundThread();

            eventHandler.Verify(x=> x(builder.TestSubject, EventArgs.Empty), Times.Once);            
        }

        [TestMethod]
        public void WhenBindingChanges_AnalysisIsRequested()
        {
            var builder = new TestEnvironmentBuilder();

            builder.SimulateBindingChanged();

            // Should re-analyse
            builder.AssertAnalysisIsRequested();
            builder.AssertSwitchedToBackgroundThread();
            builder.Logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_BindingChanged);
        }

        [TestMethod]
        public void WhenBindingChanges_HasSubscribersToConfigChangedEvent_SubscribersNotified()
        {
            var builder = new TestEnvironmentBuilder();
            var eventHandler = new Mock<EventHandler>();
            builder.TestSubject.ConfigChanged += eventHandler.Object;

            builder.SimulateBindingChanged();
            builder.AssertSwitchedToBackgroundThread();

            eventHandler.Verify(x => x(builder.TestSubject, EventArgs.Empty), Times.Once);
        }

        [TestMethod]
        public void WhenQualityProfilesChanged_AnalysisIsRequested()
        {
            var builder = new TestEnvironmentBuilder();

            builder.SimulateQualityProfilesChanged();

            // Should re-analyse
            builder.AssertAnalysisIsRequested();
            builder.AssertSwitchedToBackgroundThread();
            builder.Logger.AssertOutputStringExists(AnalysisStrings.ConfigMonitor_QualityProfilesChanged);
        }

        [TestMethod]
        public void WhenQualityProfilesChanged_HasSubscribersToConfigChangedEvent_SubscribersNotified()
        {
            var builder = new TestEnvironmentBuilder();
            var eventHandler = new Mock<EventHandler>();
            builder.TestSubject.ConfigChanged += eventHandler.Object;

            builder.SimulateQualityProfilesChanged();
            builder.AssertSwitchedToBackgroundThread();

            eventHandler.Verify(x => x(builder.TestSubject, EventArgs.Empty), Times.Once);
        }

        [TestMethod]
        public void WhenDisposed_EventsAreIgnored()
        {
            var builder = new TestEnvironmentBuilder();

            // Act
            builder.TestSubject.Dispose();

            // Raise events and check they are ignored
            builder.SimulateUserSettingsChanged();
            builder.SimulateBindingChanged();
            builder.SimulateQualityProfilesChanged();
            builder.AssertAnalysisIsNotRequested();
        }

        private class TestEnvironmentBuilder
        {
            private readonly Mock<IAnalysisRequester> analysisRequesterMock;
            private readonly Mock<IUserSettingsProvider> userSettingsProviderMock;
            private readonly Mock<IActiveSolutionBoundTracker> activeSolutionBoundTracker;
            private readonly Mock<INotifyQualityProfilesChanged> notifyQPsUpdated;
            private readonly Mock<IThreadHandling> threadHandling;

            public TestEnvironmentBuilder()
            {
                analysisRequesterMock = new Mock<IAnalysisRequester>();
                userSettingsProviderMock = new Mock<IUserSettingsProvider>();
                activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
                notifyQPsUpdated = new Mock<INotifyQualityProfilesChanged>();
                threadHandling = new Mock<IThreadHandling>();
                
                threadHandling.Setup(th => th.SwitchToBackgroundThread()).Returns(new NoOpAwaitable());

                Logger = new TestLogger();

                TestSubject = new AnalysisConfigMonitor(analysisRequesterMock.Object,
                    userSettingsProviderMock.Object, activeSolutionBoundTracker.Object, notifyQPsUpdated.Object, Logger, threadHandling.Object);
            }

            public TestLogger Logger { get; }

            public AnalysisConfigMonitor TestSubject { get; }

            public void SimulateUserSettingsChanged()
                => userSettingsProviderMock.Raise(x => x.SettingsChanged += null, EventArgs.Empty);

            public void SimulateBindingChanged(BindingConfiguration config = null)
                => activeSolutionBoundTracker.Raise(x => x.SolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(config));

            public void SimulateQualityProfilesChanged()
                => notifyQPsUpdated.Raise(x => x.QualityProfilesChanged += null, EventArgs.Empty);

            public void AssertAnalysisIsRequested()
                => analysisRequesterMock.Verify(x => x.RequestAnalysis((IAnalyzerOptions)null, Array.Empty<string>()), Times.Once);

            public void AssertAnalysisIsNotRequested()
                => analysisRequesterMock.Invocations.Count.Should().Be(0);
    
            public void AssertSwitchedToBackgroundThread()
                => threadHandling.Verify(x => x.SwitchToBackgroundThread(), Times.Once);
        }
    }
}
