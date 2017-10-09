/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Suppression;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.UnitTests.Suppression
{
    [TestClass]
    public class SonarQubeIssueProviderTests
    {
        private Mock<ISonarQubeService> mockSqService;
        private Mock<IActiveSolutionBoundTracker> mockTracker;
        private Mock<ITimerFactory> mockTimerFactory;
        private Mock<ITimer> mockTimer;

        /// <summary>
        /// Wired up to the Timer.Start/Stop mock methods to track the current timer status
        /// </summary>
        private bool timerRunning;

        [TestInitialize]
        public void TestInitialize()
        {
            mockSqService = new Mock<ISonarQubeService>();
            mockTracker = new Mock<IActiveSolutionBoundTracker>();

            mockTimerFactory = new Mock<ITimerFactory>();
            mockTimer = new Mock<ITimer>();

            mockTimerFactory.Setup(x => x.Create())
                .Returns(mockTimer.Object)
                .Verifiable();

            mockTimer.SetupSet(x => x.Interval = It.IsInRange(1d, double.MaxValue, Range.Inclusive)).Verifiable();
            mockTimer.Setup(x => x.Start()).Callback(() => timerRunning = true);
            mockTimer.Setup(x => x.Stop()).Callback(() => timerRunning = false);
            mockTimer.Setup(x => x.Dispose()).Callback(() => timerRunning = false);
        }

        [TestMethod]
        public void Constructor_ThrowsOnNull()
        {
            Action op = () => new SonarQubeIssuesProvider(null, mockTracker.Object, mockTimerFactory.Object);
            op.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("sonarQubeService");

            op = () => new SonarQubeIssuesProvider(mockSqService.Object, null, mockTimerFactory.Object);
            op.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("solutionBoundTracker");

            op = () => new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, null);
            op.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("timerFactory");
        }

        [TestMethod]
        public void Constructor_TimerIsInitialized()
        {
            // Arrange
            SetupSolutionBinding(true /* is bound */, false /* is connected */, null, null);

            // 1. Construction -> timer initialised
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);

            // Assert
            mockTimerFactory.VerifyAll();
            VerifyTimerStart(Times.Once());
            timerRunning.Should().Be(true);
            VerifyServiceIsConnected(Times.Once()); // bound -> check connection status...
            VerifyServiceGetIssues(Times.Never());  // ... but don't try to synch since not connected

            // 2. Timer event raised -> check attempt is made to synchronize data
            RaiseTimerElapsed(DateTime.UtcNow);

            VerifyServiceIsConnected(Times.Exactly(2));
            VerifyServiceGetIssues(Times.Never()); // still not connected so can't get data
            timerRunning.Should().Be(true);
        }

        [TestMethod]
        public void Constructor_UnboundSolution_DoesNotSyncOrStartMonitoring()
        {
            // Arrange
            SetupSolutionBinding(false /* is bound */, false /* is connected */, null, null);

            // Act
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);

            // Assert - issues are not fetched and timer is not started
            VerifyServiceGetIssues(Times.Never());
            VerifyTimerStart(Times.Never());
            timerRunning.Should().Be(false);
        }

        [TestMethod]
        public void Constructor_BoundSolution_SyncsAndStartsMonitoring()
        {
            // Arrange
            SetupSolutionBinding(true /* is bound */, true /* is connected */, "keyXXX", new List<SonarQubeIssue>());

            // Act
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);

            // Assert - issues are fetched and timer is started
            VerifyServiceGetIssues(Times.Once());
            VerifyTimerStart(Times.Once());
            timerRunning.Should().Be(true);
        }

        [TestMethod]
        public void Dispose_UnboundSolution_NoError()
        {
            // Arrange
            SetupSolutionBinding(false /* is bound */, false /* is connected */, null, null);
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);

            // 1. Dispose
            issuesProvider.Dispose();
            issuesProvider.Dispose();
            issuesProvider.Dispose();

            // Assert
            mockTimer.Verify(x => x.Dispose(), Times.Once);

            // 2. Check solution events are no longer being tracked
            mockSqService.ResetCalls();
            RaiseSolutionBoundEvent(true, "keyABC");
            VerifyServiceGetIssues(Times.Never());
        }

        [TestMethod]
        public void Dispose_BoundSolution_StopsMonitoring()
        {
            // Arrange
            SetupSolutionBinding(true /* is bound */, true /* is connected */, "keyXXX", new List<SonarQubeIssue>());
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);

            // 1. Dispose
            issuesProvider.Dispose();
            issuesProvider.Dispose();
            issuesProvider.Dispose();

            // Assert
            mockTimer.Verify(x => x.Dispose(), Times.Once);

            // 2. Check solution events are no longer being tracked
            mockSqService.ResetCalls();
            RaiseSolutionBoundEvent(true, "keyABC");
            VerifyServiceGetIssues(Times.Never());
        }

        [TestMethod]
        public void Event_OnSolutionBecomingUnbound_StopsTimer()
        {
            // 1. Bound and connected initially
            SetupSolutionBinding(true /* is bound */, true /* is connected */, "keyXXX", new List<SonarQubeIssue>());

            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);

            VerifyServiceGetIssues(Times.Once());
            VerifyTimerStart(Times.Once());
            timerRunning.Should().Be(true);

            // 2. Event -> unbound solution
            SetupSolutionBinding(false /* is bound */, false /* is connected */, null, null);

            mockTracker.Raise(e => e.SolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(false, null));

            VerifyServiceGetIssues(Times.Once()); // issues not fetched again
            VerifyTimerStop(Times.Once());
            timerRunning.Should().Be(false);
        }

        [TestMethod]
        public void Event_OnSolutionBecomingBound_SynchronizesAndStartsTimer()
        {
            // 1. Initially not bound
            SetupSolutionBinding(false /* is bound */, false/* is connected */, null, null);

            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);

            VerifyServiceGetIssues(Times.Never());
            VerifyTimerStart(Times.Never());
            timerRunning.Should().Be(false);

            // 2. Event -> unbound solution
            SetupSolutionBinding(true /* is bound */, true /* is connected */, "keyYYY", new List<SonarQubeIssue>());

            RaiseSolutionBoundEvent(true, "keyYYY");

            VerifyServiceGetIssues(Times.Once());
            VerifyTimerStart(Times.Once());
            timerRunning.Should().Be(true);
        }

        #region Fetching issues

        // TODO: tests for GetSuppressedIssues

        #endregion

        /// <summary>
        /// Configures the mock tracker and service to return the specified values
        /// </summary>
        private void SetupSolutionBinding(bool isBound, bool isConnected, string projectKey, IList<SonarQubeIssue> issuesToReturn)
        {
            mockTracker.Setup(t => t.IsActiveSolutionBound).Returns(isBound).Verifiable();
            mockTracker.SetupGet(t => t.ProjectKey).Returns(projectKey).Verifiable();

            mockSqService.Setup(x => x.IsConnected).Returns(isConnected).Verifiable();
            
            mockSqService.Setup(x => x.GetSuppressedIssuesAsync(projectKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(issuesToReturn)
                .Verifiable();
        }

        private void VerifyTimerStart(Times expected)
        {
            mockTimer.Verify(t => t.Start(), expected);
        }

        private void VerifyTimerStop(Times expected)
        {
            mockTimer.Verify(t => t.Stop(), expected);
        }

        private void VerifyServiceGetIssues(Times expected)
        {
            mockSqService.Verify(x => x.GetSuppressedIssuesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), expected);
        }

        private void VerifyServiceIsConnected(Times expected)
        {
            mockSqService.Verify(x => x.IsConnected, expected);
        }

        private void RaiseTimerElapsed(DateTime eventTime)
        {
            mockTimer.Raise(t => t.Elapsed += null, new TimerEventArgs(eventTime));
        }

        private void RaiseSolutionBoundEvent(bool isBound, string projectKey)
        {
            mockTracker.Raise(e => e.SolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(isBound, projectKey));
        }
    }
}
