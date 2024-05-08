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

using SonarLint.VisualStudio.ConnectedMode.Hotspots;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.SystemAbstractions;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Suppressions
{
    [TestClass]
    public class TimedUpdateHandlerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TimedUpdateHandler, TimedUpdateHandler>(
                MefTestHelpers.CreateExport<ISuppressionIssueStoreUpdater>(),
                MefTestHelpers.CreateExport<IServerHotspotStoreUpdater>(),
                MefTestHelpers.CreateExport<IQualityProfileUpdater>(),
                MefTestHelpers.CreateExport<ILogger>(),
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<TimedUpdateHandler>();
        }

        [TestMethod]
        public void Ctor_TimerIsSetupAsExpected()
        {
            var refreshTimer = new Mock<ITimer>();
            refreshTimer.SetupProperty(x => x.AutoReset);
            refreshTimer.SetupProperty(x => x.Interval);

            var timerFactory = CreateTimerFactory(refreshTimer.Object);
            var activeSolutionBoundTracker = CreateActiveSolutionBoundTrackerWihtBindingConfig(SonarLintMode.Connected);

            _ = CreateTestSubject(activeSolutionBoundTracker: activeSolutionBoundTracker.Object, timerFactory: timerFactory);

            refreshTimer.Object.AutoReset.Should().BeTrue();
            refreshTimer.Object.Interval.Should().Be(1000 * 60 * 10);
            refreshTimer.VerifyAdd(x => x.Elapsed += It.IsAny<EventHandler<TimerEventArgs>>(), Times.Once);
            refreshTimer.Verify(x => x.Start(), Times.Once);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Standalone, false)]
        [DataRow(SonarLintMode.Connected, true)]
        [DataRow(SonarLintMode.LegacyConnected, true)]
        public void Ctor_DependingOnBindingConfig_InitialTimeStateSetCorrectly(SonarLintMode mode, bool start)
        {
            var refreshTimer = new Mock<ITimer>();
            var timerFactory = CreateTimerFactory(refreshTimer.Object);
            var activeSolutionBoundTracker = CreateActiveSolutionBoundTrackerWihtBindingConfig(mode);

            _ = CreateTestSubject(activeSolutionBoundTracker: activeSolutionBoundTracker.Object, timerFactory: timerFactory);

            refreshTimer.Verify(x => x.Start(), start ? Times.Once : Times.Never);
            refreshTimer.Verify(x => x.Stop(), start ? Times.Never : Times.Once );
        }

        [TestMethod]
        public void InvokeEvent_TimerElapsed_StoreUpdatersAreCalled()
        {
            var refreshTimer = new Mock<ITimer>();
            var timerFactory = CreateTimerFactory(refreshTimer.Object);
            var activeSolutionBoundTracker = CreateActiveSolutionBoundTrackerWihtBindingConfig(SonarLintMode.Connected);
            var suppressionIssueStoreUpdater = new Mock<ISuppressionIssueStoreUpdater>();
            var serverHotspotStoreUpdater = new Mock<IServerHotspotStoreUpdater>();
            var qualityProfileUpdater = new Mock<IQualityProfileUpdater>();

            _ = CreateTestSubject(activeSolutionBoundTracker.Object, suppressionIssueStoreUpdater.Object, serverHotspotStoreUpdater.Object, qualityProfileUpdater.Object, timerFactory);

            refreshTimer.Raise(x => x.Elapsed += null, new TimerEventArgs(DateTime.UtcNow));

            suppressionIssueStoreUpdater.Verify(x => x.UpdateAllServerSuppressionsAsync(), Times.Once);
            serverHotspotStoreUpdater.Verify(x => x.UpdateAllServerHotspotsAsync(), Times.Once);
            qualityProfileUpdater.Verify(x => x.UpdateAsync(), Times.Once);
        }

        [TestMethod]
        public void Dispose_RefreshTimerDisposed_RaisingEventDoesNothing()
        {
            var refreshTimer = new Mock<ITimer>();
            var timerFactory = CreateTimerFactory(refreshTimer.Object);
            var activeSolutionBoundTracker = CreateActiveSolutionBoundTrackerWihtBindingConfig(SonarLintMode.Connected);
            var suppressionIssueStoreUpdater = new Mock<ISuppressionIssueStoreUpdater>();
            var serverHotspotStoreUpdater = new Mock<IServerHotspotStoreUpdater>();
            var qualityProfileUpdater = new Mock<IQualityProfileUpdater>();

            var testSubject = CreateTestSubject(activeSolutionBoundTracker.Object, suppressionIssueStoreUpdater.Object, serverHotspotStoreUpdater.Object, qualityProfileUpdater.Object, timerFactory);

            testSubject.Dispose();

            refreshTimer.Verify(x => x.Dispose(), Times.Once);
            refreshTimer.Raise(x => x.Elapsed += null, new TimerEventArgs(DateTime.UtcNow));

            suppressionIssueStoreUpdater.Verify(x => x.UpdateAllServerSuppressionsAsync(), Times.Never);
            serverHotspotStoreUpdater.Verify(x => x.UpdateAllServerHotspotsAsync(), Times.Never);
            qualityProfileUpdater.Verify(x => x.UpdateAsync(), Times.Never);
        }

        [TestMethod]
        public void InvokeBindingChanged_TimerStartsStopsOnActiveSolutionBoundChange()
        {
            var refreshTimer = new Mock<ITimer>();
            var timerFactory = CreateTimerFactory(refreshTimer.Object);
            var activeSolutionBoundTracker = CreateActiveSolutionBoundTrackerWihtBindingConfig(SonarLintMode.Standalone);

            _ = CreateTestSubject(activeSolutionBoundTracker:activeSolutionBoundTracker.Object, timerFactory:timerFactory);

            refreshTimer.Reset();

            activeSolutionBoundTracker.Raise(x => x.SolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(CreateBindingConfiguration(SonarLintMode.Connected)));
            refreshTimer.Verify(x => x.Start(), Times.Once);
            refreshTimer.Verify(x => x.Stop(), Times.Never);

            activeSolutionBoundTracker.Raise(x => x.SolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(CreateBindingConfiguration(SonarLintMode.Standalone)));
            refreshTimer.Verify(x => x.Start(), Times.Once);
            refreshTimer.Verify(x => x.Stop(), Times.Once);
        }

        private static ITimerFactory CreateTimerFactory(ITimer timer)
        {
            var timerFactory = new Mock<ITimerFactory>();

            timerFactory.Setup(x => x.Create()).Returns(timer);

            return timerFactory.Object;
        }

        private static TimedUpdateHandler CreateTestSubject(IActiveSolutionBoundTracker activeSolutionBoundTracker, 
            ISuppressionIssueStoreUpdater suppressionIssueStoreUpdater = null,
            IServerHotspotStoreUpdater serverHotspotStoreUpdater = null ,
            IQualityProfileUpdater qualityProfileUpdater = null,
            ITimerFactory timerFactory = null)
        {
            return new TimedUpdateHandler(
                suppressionIssueStoreUpdater ?? Mock.Of<ISuppressionIssueStoreUpdater>(),
                serverHotspotStoreUpdater ?? Mock.Of<IServerHotspotStoreUpdater>(),
                qualityProfileUpdater ?? Mock.Of<IQualityProfileUpdater>(),
                activeSolutionBoundTracker,
                new TestLogger(logToConsole: true),
                timerFactory ?? Mock.Of<ITimerFactory>());
        }

        private BindingConfiguration CreateBindingConfiguration(SonarLintMode mode)
        {
            return new BindingConfiguration(new BoundSonarQubeProject(new Uri("http://localhost"), "test", ""), mode, "");
        }

        private Mock<IActiveSolutionBoundTracker> CreateActiveSolutionBoundTrackerWihtBindingConfig(SonarLintMode mode)
        {
            var activeSolutionTracker = new Mock<IActiveSolutionBoundTracker>();

            var bindingConfig = CreateBindingConfiguration(mode);

            activeSolutionTracker.Setup(x => x.CurrentConfiguration).Returns(bindingConfig);

            return activeSolutionTracker;
        }
    }
}
