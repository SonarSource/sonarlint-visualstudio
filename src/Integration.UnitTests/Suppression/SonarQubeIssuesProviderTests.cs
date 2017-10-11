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
using System.Diagnostics;
using System.Linq;
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
        /// Wait handle that is set to signalled when the initial fetch task has started
        /// </summary>
        private EventWaitHandle InitialFetchWaitHandle;

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
            SetupSolutionBinding(isBound: true, isConnected: false, projectKey: null, issues: null);
            mockTimer.SetupSet(t => t.AutoReset = true).Verifiable();

            // 1. Construction -> timer initialised
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);

            // Assert
            mockTimerFactory.VerifyAll();
            mockTimer.VerifySet(t => t.AutoReset = true, Times.Once);
            VerifyTimerStart(Times.Once());
            timerRunning.Should().Be(true);

            // The call to service.IsConnected is on a background thread so we can't
            // reliably check whether it has occurred yet. However, we know that 
            // service.GetIssues(...) should never have been called since the service
            // is not connected.
            VerifyServiceGetIssues(Times.Never());

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
            SetupSolutionBinding(isBound: false, isConnected: false, projectKey: null, issues: null);

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
            SetupSolutionBinding(isBound: true, isConnected: true, projectKey: "keyXXX", issues: new List<SonarQubeIssue>());
            
            // Act
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);
            WaitForInitialFetchTaskToStart();            

            // Assert - issues are fetched and timer is started
            VerifyTimerStart(Times.Once());
            timerRunning.Should().Be(true);
            VerifyServiceGetIssues(Times.Once());
        }

        [TestMethod]
        public void Dispose_UnboundSolution_NoError()
        {
            // Arrange
            SetupSolutionBinding(isBound: false, isConnected: false, projectKey: null, issues: null);

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
            SetupSolutionBinding(isBound: true, isConnected: true, projectKey: "keyXXX", issues: new List<SonarQubeIssue>());

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
            SetupSolutionBinding(isBound: true, isConnected: true, projectKey: "keyXXX", issues: new List<SonarQubeIssue>());

            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);
            WaitForInitialFetchTaskToStart();

            VerifyServiceGetIssues(Times.Once());
            VerifyTimerStart(Times.Once());
            timerRunning.Should().Be(true);

            // 2. Event -> unbound solution
            SetupSolutionBinding(isBound: false, isConnected: false, projectKey: null, issues: null);

            mockTracker.Raise(e => e.SolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(false, null));

            VerifyServiceGetIssues(Times.Once()); // issues not fetched again
            VerifyTimerStop(Times.Once());
            timerRunning.Should().Be(false);
        }

        [TestMethod]
        public void Event_OnSolutionBecomingBound_SynchronizesAndStartsTimer()
        {
            // 1. Initially not bound
            SetupSolutionBinding(isBound: false, isConnected: false, projectKey: null, issues: null);

            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);

            VerifyServiceGetIssues(Times.Never());
            VerifyTimerStart(Times.Never());
            timerRunning.Should().Be(false);

            // 2. Event -> bound solution
            SetupSolutionBinding(isBound: true, isConnected: true, projectKey: "keyXXX", issues: new List<SonarQubeIssue>());

            RaiseSolutionBoundEvent(true, "keyYYY");

            VerifyServiceGetIssues(Times.Once());
            VerifyTimerStart(Times.Once());
            timerRunning.Should().Be(true);
        }

        #region Fetching issues

        [TestMethod]
        public void GetIssues_NoIssuesOnServer_ReturnsEmptyList()
        {
            SetupSolutionBinding(isBound: true, isConnected: true, projectKey: "sqkey", issues: null);

            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);

            // 1. SonarQube project key doesn't match -> no issues
            var matches = issuesProvider.GetSuppressedIssues("any project", "any file");
            matches.Should().NotBeNull();
            matches.Should().BeEmpty();

            // Cached issues should be used after first fetch. Should not refetch just
            // because the initial fetch returned no items.
            VerifyServiceGetIssues(Times.Exactly(1));
        }

        [TestMethod]
        public void GetIssues_IssuesExists_FiltersByFileAndProject()
        {
            var issue1 = new SonarQubeIssue("folder1/file1", "hash1", 0, "message", "sqkey:sqkey:projectID1",
                SonarQubeIssueResolutionState.FalsePositive, "S101");
            var issue2 = new SonarQubeIssue("folder1/file1", "hash2", 0, "message", "sqkey:sqkey:projectID2",
                SonarQubeIssueResolutionState.WontFix, "S102");
            var issue3 = new SonarQubeIssue("folder1/file1", "hash3", 0, "message", "sqkey:sqkey:projectID2",
                SonarQubeIssueResolutionState.Fixed, "S103");
            var issue4 = new SonarQubeIssue("folder1/file1", "hash4", 0, "message", "sqkey:sqkey: projectID2",
                SonarQubeIssueResolutionState.WontFix, "S104");
            var issue5 = new SonarQubeIssue("folder1/file1", "hash5", 0, "message", "sqkey:XXX:projectID2",
                SonarQubeIssueResolutionState.FalsePositive, "S105");

            var issue6 = new SonarQubeIssue("folder1/file2", "hash6", 0, "message", "sqkey:sqkey:projectID1",
                SonarQubeIssueResolutionState.Unresolved, "S106");
            var issue7 = new SonarQubeIssue("folder2/file1", "hash7", 0, "message", "sqkey:sqkey:projectID1",
                SonarQubeIssueResolutionState.Fixed, "S107");

            var issue8 = new SonarQubeIssue("file1", "hash8", 0, "message", "sqkey:sqkey:projectID1",
                SonarQubeIssueResolutionState.FalsePositive, "S108");
            var issue9 = new SonarQubeIssue("file1", "hash9", 0, "message", "sqkey:sqkey:projectID1xxx",
                SonarQubeIssueResolutionState.WontFix, "S109");
            var issue10 = new SonarQubeIssue("file1", "hash10", 0, "message", "sqkey:sqkey:projectID",
                SonarQubeIssueResolutionState.Unresolved, "S110");

            SetupSolutionBinding(isBound: true, isConnected: true, projectKey: "sqKEY",
                issues: new List<SonarQubeIssue> { issue1, issue2, issue3, issue4, issue5, issue6, issue7, issue8, issue9, issue1 });

            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);

            // 1. Project id doesn't match -> no issues
            var matches = issuesProvider.GetSuppressedIssues("unrecognisedProjectId", "folder1/file1");
            matches.Should().BeEmpty();
            VerifyServiceGetIssues(Times.Exactly(1)); // cached issues should be used

            // 2. File id doesn't match -> no issues
            matches = issuesProvider.GetSuppressedIssues("projectID1", "folder1/filexxx");
            matches.Should().BeEmpty();

            // 3. File id and guid match -> issues returned
            matches = issuesProvider.GetSuppressedIssues("projectID1", "file1");
            matches.Count().Should().Be(1);
            CheckExpectedIssueReturned("hash8", matches);

            // 4. File id and guid match, case-insensitive -> issues returned
            matches = issuesProvider.GetSuppressedIssues("PROJECTID2", "FOLDER1/FILE1");
            matches.Count().Should().Be(2);
            CheckExpectedIssueReturned("hash2", matches);
            CheckExpectedIssueReturned("hash3", matches);

            VerifyServiceGetIssues(Times.Exactly(1));
        }

        [TestMethod]
        public void GetIssues_IssuesExists_DifferentSQProjectKey_NoMatches()
        {
            var issue1 = new SonarQubeIssue("file1", "hash1", 0, "message", "sqkey:sqkey:projectID1",
                SonarQubeIssueResolutionState.FalsePositive, "S101");
            var issue2 = new SonarQubeIssue("folder1/file1", "hash2", 0, "message", "sqkey:sqkey:projectID2",
                SonarQubeIssueResolutionState.FalsePositive, "S102");

            SetupSolutionBinding(isBound: true, isConnected: true, projectKey: "otherkey",
                issues: new List<SonarQubeIssue> { issue1, issue2});

            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);

            // 1. SonarQube project key doesn't match -> no issues
            var matches = issuesProvider.GetSuppressedIssues("projectID1", "file1");
            matches.Should().BeEmpty();

            // 2. SonarQube project key doesn't match -> no issues
            matches = issuesProvider.GetSuppressedIssues("PROJECTID2", "folder1/file1");
            matches.Should().BeEmpty();

            VerifyServiceGetIssues(Times.Exactly(1)); // cached issues should be used
        }

        [TestMethod]
        public void GetIssues_IssuesNotYetFetch_WaitsForIssuesToBeFetched()
        {
            var issue1 = new SonarQubeIssue("folder1/file1", "hash1", 0, "message", "sqkey:sqkey:projectID1",
                SonarQubeIssueResolutionState.FalsePositive, "S101");

            EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            int callbackCount = 0;
            bool callbackCompleted = false;
            Func<IList<SonarQubeIssue>> serviceFetchIssuesTask = () =>
            {
                callbackCount++;

                waitHandle.Set(); // signal so the test can continue

                Thread.Sleep(Debugger.IsAttached ? 5000 : 500);
                callbackCompleted = true;
                return new List<SonarQubeIssue> { issue1 };
            };

            SetupSolutionBinding(isBound: true, isConnected: true, projectKey: "sqkey", issues: new List<SonarQubeIssue> { issue1 });
            mockSqService.Setup(x => x.GetSuppressedIssuesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceFetchIssuesTask)
                .Verifiable();

            // 1. Create the issue provider
            // The initial fetch should be triggered, but not yet completed
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);

            var waitSignaled = waitHandle.WaitOne(Debugger.IsAttached ? 20000 : 5000); // wait for fetch to start...
            waitSignaled.Should().BeTrue(); // error - fetch has not started running

            // 2. Now request the issues - should wait until the issues have been retrieved
            var matches = issuesProvider.GetSuppressedIssues("projectid1", "folder1/file1");

            VerifyServiceGetIssues(Times.Once());
            callbackCount.Should().Be(1);
            callbackCompleted.Should().BeTrue();
            matches.Count().Should().Be(1);
            CheckExpectedIssueReturned("hash1", matches);

            // 3. Now fetch again - should not wait again
            matches = issuesProvider.GetSuppressedIssues("folder1/file1", "projectid1");

            VerifyServiceGetIssues(Times.Once());
            callbackCount.Should().Be(1);
        }

        [TestMethod]
        public void GetIssues_UnboundSolution_NoErrors()
        {
            // Arrange
            SetupSolutionBinding(isBound: false, isConnected: false, projectKey: null, issues: null);

            // Act
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);
            var matches = issuesProvider.GetSuppressedIssues("any id", "any file");

            // Assert - issues are not fetched
            VerifyServiceGetIssues(Times.Never());
            matches.Should().BeEmpty();
        }

        [TestMethod]
        public void GetIssues_ErrorInInitialFetchTask_IsSuppressed()
        {
            var issue1 = new SonarQubeIssue("folder1/file1", "hash1", 0, "message", "sqkey:sqkey:projectID1",
                SonarQubeIssueResolutionState.FalsePositive, "S101");

            EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            Func<IList<SonarQubeIssue>> serviceFetchIssuesTask = () =>
            {
                waitHandle.Set(); // signal so the test can continue
                throw new ApplicationException("dummy error from mock");
            };

            SetupSolutionBinding(isBound: true, isConnected: true, projectKey: "sqkey", issues: new List<SonarQubeIssue> { issue1 });
            mockSqService.Setup(x => x.GetSuppressedIssuesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceFetchIssuesTask)
                .Verifiable();

            // 1. Create the issue provider
            // The initial fetch should be triggered, but not yet completed
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);

            var waitSignaled = waitHandle.WaitOne(Debugger.IsAttached ? 20000 : 5000); // wait for fetch to start...
            waitSignaled.Should().BeTrue(); // error - fetch has not started running

            // 2. Now request the issues - task completes with an error
            var matches = issuesProvider.GetSuppressedIssues("projectid1", "folder1/file1");

            VerifyServiceGetIssues(Times.Once());
            matches.Should().BeEmpty();

            // 3. Now fetch again - should not wait again, should not error
            matches = issuesProvider.GetSuppressedIssues("folder1/file1", "projectid1");
            matches.Should().BeEmpty();
            VerifyServiceGetIssues(Times.Once());
        }

        [TestMethod]
        public void GetIssues_ErrorFetchingOnBindingChanged_IsSuppressed()
        {
            ExecuteGetIssuesOnTriggerWithError("projectId", () => RaiseSolutionBoundEvent(true, "projectId"));
            VerifyTimerStart(Times.Exactly(2)); // once on construction, once in the refresh 
        }

        [TestMethod]
        public void GetIssues_ErrorFetchingOnTimerElapsed_IsSuppressed()
        {
            ExecuteGetIssuesOnTriggerWithError("projectId", () => RaiseTimerElapsed(DateTime.UtcNow));
            VerifyTimerStart(Times.Exactly(1)); // once, on construction
        }

        private void ExecuteGetIssuesOnTriggerWithError(string projectId, Action triggerFetchIssues)
        {
            // Tests that the triggers that cause the data to be refetched won't propagate errors
            // if the GetIssues call throws.

            var issue1 = new SonarQubeIssue("folder1/file1", "hash1", 0, "message", "sqkey:sqkey:" + projectId,
                SonarQubeIssueResolutionState.FalsePositive, "S101");
            SetupSolutionBinding(isBound: true, isConnected: true, projectKey: "sqkey", issues: new List<SonarQubeIssue> { issue1 });

            // 1. Create the issue provider and call GetIssues to make sure the issues are cached
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, mockTracker.Object, mockTimerFactory.Object);
            var matches = issuesProvider.GetSuppressedIssues(projectId, "folder1/file1");
            VerifyServiceGetIssues(Times.Once());
            matches.Count().Should().Be(1);

            // 2. Configure service to throw, then execute the fetch trigger
            int fetchCallCount = 0;
            Func<IList<SonarQubeIssue>> serviceFetchIssuesTask = () =>
            {
                fetchCallCount++;
                throw new ApplicationException("dummy error from mock");
            };

            mockSqService.Setup(x => x.GetSuppressedIssuesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceFetchIssuesTask)
                .Verifiable();

            triggerFetchIssues();

            // 3. Fetch issues again - should used cached issues
            matches = issuesProvider.GetSuppressedIssues(projectId, "folder1/file1");
            VerifyServiceGetIssues(Times.Exactly(2));
            matches.Count().Should().Be(1);
            fetchCallCount.Should().Be(1);
        }

        #endregion

        /// <summary>
        /// Configures the mock tracker and service to return the specified values
        /// </summary>
        private void SetupSolutionBinding(bool isBound, bool isConnected, string projectKey, IList<SonarQubeIssue> issues)
        {
            mockTracker.Setup(t => t.IsActiveSolutionBound).Returns(isBound).Verifiable();
            mockTracker.SetupGet(t => t.ProjectKey).Returns(projectKey).Verifiable();

            mockSqService.Setup(x => x.IsConnected).Returns(isConnected).Verifiable();

            if (isConnected)
            {
                InitialFetchWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            }

            Func<IList<SonarQubeIssue>> serviceFetchIssuesTask = () =>
            {
                InitialFetchWaitHandle?.Set(); // signal so the test can continue
                return issues;
            };

            mockSqService.Setup(x => x.GetSuppressedIssuesAsync(projectKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceFetchIssuesTask)
                .Verifiable();
        }

        private void WaitForInitialFetchTaskToStart()
        {
            // Only applicable for solutions that are both connected and bound
            InitialFetchWaitHandle.Should().NotBeNull("it should have been initialised by calling SetupSolutionBinding");
            var waitSignaled = InitialFetchWaitHandle.WaitOne(Debugger.IsAttached ? 20000 : 5000); // wait for fetch to start...
            waitSignaled.Should().BeTrue(); // error - fetch has not started running
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

        private static void CheckExpectedIssueReturned(string expectedHash, IEnumerable<SonarQubeIssue> actualIssues)
        {
            SonarQubeIssue match = actualIssues.FirstOrDefault(i => i.Hash.Equals(expectedHash, StringComparison.InvariantCultureIgnoreCase));
            match.Should().NotBeNull();
        }
    }
}
