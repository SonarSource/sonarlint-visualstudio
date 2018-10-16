/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
        private Mock<ITimerFactory> mockTimerFactory;
        private Mock<ITimer> mockTimer;
        private TestLogger testLogger;

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

            testLogger = new TestLogger();

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
            string validProjectKey = "key1";

            Action op = () => new SonarQubeIssuesProvider(null, validProjectKey, mockTimerFactory.Object, testLogger);
            op.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sonarQubeService");

            op = () => new SonarQubeIssuesProvider(mockSqService.Object, null, mockTimerFactory.Object, testLogger);
            op.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sonarQubeProjectKey");

            op = () => new SonarQubeIssuesProvider(mockSqService.Object, "", mockTimerFactory.Object, testLogger);
            op.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sonarQubeProjectKey");

            op = () => new SonarQubeIssuesProvider(mockSqService.Object, "\r\n ", mockTimerFactory.Object, testLogger);
            op.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sonarQubeProjectKey");

            op = () => new SonarQubeIssuesProvider(mockSqService.Object, validProjectKey, null, testLogger);
            op.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("timerFactory");

            op = () => new SonarQubeIssuesProvider(mockSqService.Object, validProjectKey, mockTimerFactory.Object, null);
            op.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void Constructor_TimerIsInitialized()
        {
            // Arrange
            // The provider will attempt to fetch issues at start up and will loop if it
            // is not connected to the server, so we'll initialise it as connected.
            SetupSolutionBinding(isConnected: true, issues: null);
            mockTimer.SetupSet(t => t.AutoReset = true).Verifiable();

            // 1. Construction -> timer initialised
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, "sqKey", mockTimerFactory.Object,
                testLogger);

            // Assert
            mockTimerFactory.VerifyAll();
            mockTimer.VerifySet(t => t.AutoReset = true, Times.Once);
            VerifyTimerStart(Times.Once());
            timerRunning.Should().Be(true);

            WaitForInitialFetchTaskToStart();
            VerifyServiceIsConnected(Times.Exactly(2));
            VerifyServiceGetIssues(Times.Once());

            // 2. Timer event raised -> check attempt is made to synchronize data
            RaiseTimerElapsed(DateTime.UtcNow);

            VerifyServiceIsConnected(Times.Exactly(3));
            VerifyServiceGetIssues(Times.Exactly(2));
            timerRunning.Should().Be(true);

        }

        [TestMethod]
        public void Constructor_Connected_StartsMonitoringAndSyncs()
        {
            // Arrange
            SetupSolutionBinding(isConnected: true, issues: new List<SonarQubeIssue>());

            // Act
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, "keyXXX",
                mockTimerFactory.Object, testLogger);
            WaitForInitialFetchTaskToStart();

            // Assert - issues are fetched and timer is started
            VerifyTimerStart(Times.Once());
            timerRunning.Should().Be(true);
            VerifyServiceGetIssues(Times.Once(), "keyXXX");
        }

        [TestMethod]
        public void Dispose_Disconnected_TimerDisposed()
        {
            // Arrange
            SetupSolutionBinding(isConnected: true, issues: null);

            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, "sqKey",
                mockTimerFactory.Object, testLogger);
            WaitForInitialFetchTaskToStart();

            // Act
            issuesProvider.Dispose();
            issuesProvider.Dispose();
            issuesProvider.Dispose();

            // Assert
            mockTimer.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void Dispose_Connected_TimerDisposed()
        {
            // Arrange
            SetupSolutionBinding(isConnected: true, issues: new List<SonarQubeIssue>());

            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, "sqKey",
                mockTimerFactory.Object, testLogger);

            // Act
            issuesProvider.Dispose();
            issuesProvider.Dispose();
            issuesProvider.Dispose();

            // Assert
            mockTimer.Verify(x => x.Dispose(), Times.Once);
        }

        #region Fetching issues

        [TestMethod]
        public void GetIssues_NoIssuesOnServer_ReturnsEmptyList()
        {
            SetupSolutionBinding(isConnected: true, issues: null);

            // 1. Created -> issues fetch in background
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, "sqKey",
                mockTimerFactory.Object, testLogger);
            WaitForInitialFetchTaskToStart();

            VerifyServiceGetIssues(Times.Exactly(1), "sqKey");

            // 2. SonarQube project key doesn't match -> no issues
            var matches = issuesProvider.GetSuppressedIssues("any project", "any file");
            matches.Should().NotBeNull();
            matches.Should().BeEmpty();

            // Cached issues should be used after first fetch. Should not refetch just
            // because the initial fetch returned no items.
            VerifyServiceGetIssues(Times.Exactly(1));
        }

        [TestMethod]
        public void GetSuppressedIssues_WhenIssueIsModuleLevelAndIsFound_ReturnsExpectedIssue()
        {
            // Arrange
            var sonarQubeIssue = new SonarQubeIssue(null, null, null, "message", "sqkey:sqkey:projectId",
                SonarQubeIssueResolutionState.FalsePositive, "S101");

            SetupSolutionBinding(true, new List<SonarQubeIssue> { sonarQubeIssue });

            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, "sqKey", mockTimerFactory.Object, testLogger);
            WaitForInitialFetchTaskToStart();

            VerifyServiceGetIssues(Times.Exactly(1)); // issues should be fetched on creation

            // Act
            var matches = issuesProvider.GetSuppressedIssues("projectId", "C:\\foo\\bar.csproj");

            // Assert
            matches.Should().HaveCount(1);
        }

        [TestMethod]
        public void GetSuppressedIssues_WhenIssueIsModuleLevelAndIsNotFound_ReturnsNoIssue()
        {
            // Arrange
            var sonarQubeIssue = new SonarQubeIssue(null, null, null, "message", "sqkey:sqkey:projectId",
                SonarQubeIssueResolutionState.FalsePositive, "S101");

            SetupSolutionBinding(true, new List<SonarQubeIssue> { sonarQubeIssue });

            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, "sqKey", mockTimerFactory.Object, testLogger);
            WaitForInitialFetchTaskToStart();

            VerifyServiceGetIssues(Times.Exactly(1)); // issues should be fetched on creation

            // Act
            var matches = issuesProvider.GetSuppressedIssues("FOOBAR", "C:\\foo\\bar.csproj");

            // Assert
            matches.Should().BeEmpty();
        }

        [TestMethod]
        public void GetSuppressedIssues_WhenIssueIsFileLevelOnSubModuleAndIsFound_ReturnsExpectedIssue()
        {
            // Arrange
            var sonarQubeIssue1 = new SonarQubeIssue("\\foo\\foo.cs", null, null, "message", "sqkey:sqkey:projectId",
                SonarQubeIssueResolutionState.FalsePositive, "S1");
            var sonarQubeIssue2 = new SonarQubeIssue("/foo/foo.cs", null, null, "message", "sqkey:sqkey:projectId",
                SonarQubeIssueResolutionState.FalsePositive, "S2");
            var sonarQubeIssue3 = new SonarQubeIssue("foo\\foo.cs", null, null, "message", "sqkey:sqkey:projectId",
                SonarQubeIssueResolutionState.FalsePositive, "S3");
            var sonarQubeIssue4 = new SonarQubeIssue("foo/foo.cs", null, null, "message", "sqkey:sqkey:projectId",
                SonarQubeIssueResolutionState.FalsePositive, "S4");

            SetupSolutionBinding(true, 
                new List<SonarQubeIssue> { sonarQubeIssue1, sonarQubeIssue2, sonarQubeIssue3, sonarQubeIssue4 },
                new List<SonarQubeModule> { new SonarQubeModule("sqkey:sqkey:projectId", "", "src/bar") });

            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, "sqKey", mockTimerFactory.Object, testLogger);
            WaitForInitialFetchTaskToStart();

            VerifyServiceGetIssues(Times.Exactly(1)); // issues should be fetched on creation

            // Act
            var matches = issuesProvider.GetSuppressedIssues("", "C:\\AwesomeProject\\src\\bar\\foo\\foo.cs");

            // Assert
            matches.Should().HaveCount(4);
        }

        [TestMethod]
        public void GetSuppressedIssues_WhenIssueIsFileLevelOnSubModuleAndIsNotFound_ReturnsNoIssue()
        {
            // Arrange
            var sonarQubeIssue1 = new SonarQubeIssue("\\foo\\foo.cs", null, null, "message", "sqkey:sqkey:projectId",
                SonarQubeIssueResolutionState.FalsePositive, "S1");
            var sonarQubeIssue2 = new SonarQubeIssue("/foo/foo.cs", null, null, "message", "sqkey:sqkey:projectId",
                SonarQubeIssueResolutionState.FalsePositive, "S2");
            var sonarQubeIssue3 = new SonarQubeIssue("foo\\foo.cs", null, null, "message", "sqkey:sqkey:projectId",
                SonarQubeIssueResolutionState.FalsePositive, "S3");
            var sonarQubeIssue4 = new SonarQubeIssue("foo/foo.cs", null, null, "message", "sqkey:sqkey:projectId",
                SonarQubeIssueResolutionState.FalsePositive, "S4");

            SetupSolutionBinding(true,
                new List<SonarQubeIssue> { sonarQubeIssue1, sonarQubeIssue2, sonarQubeIssue3, sonarQubeIssue4 },
                new List<SonarQubeModule> { new SonarQubeModule("sqkey:sqkey:projectId", "", "src/bar") });

            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, "sqKey", mockTimerFactory.Object, testLogger);
            WaitForInitialFetchTaskToStart();

            VerifyServiceGetIssues(Times.Exactly(1)); // issues should be fetched on creation

            // Act / Assert - #1 - file extension is not right
            var matches = issuesProvider.GetSuppressedIssues("", "C:\\AwesomeProject\\src\\bar\\foo\\foo.vb");
            matches.Should().BeEmpty();

            // Act / Assert - #2 - path is not normalized
            matches = issuesProvider.GetSuppressedIssues("", "C:/AwesomeProject/src/bar/foo/foo.cs");
            matches.Should().BeEmpty();

            // Act / Assert - #3 - current file is one level up compared to remote file
            matches = issuesProvider.GetSuppressedIssues("", "C:\\AwesomeProject\\src\\bar\\foo.cs");
            matches.Should().BeEmpty();
        }

        [TestMethod]
        public void GetSuppressedIssues_WhenIssueIsFileLevel_MatchOnFilePathNotOnlyName()
        {
            // Arrange
            var sonarQubeIssue1 = new SonarQubeIssue("foo.cs", null, null, "message", "sqkey:sqkey:projectId",
                SonarQubeIssueResolutionState.FalsePositive, "S1");
            var sonarQubeIssue2 = new SonarQubeIssue("/foo/foo.cs", null, null, "message", "sqkey:sqkey:projectId",
                SonarQubeIssueResolutionState.FalsePositive, "S2");

            SetupSolutionBinding(true,
                new List<SonarQubeIssue> { sonarQubeIssue1, sonarQubeIssue2 },
                new List<SonarQubeModule> { new SonarQubeModule("sqkey:sqkey:projectId", "", "src/bar") });

            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, "sqKey", mockTimerFactory.Object, testLogger);
            WaitForInitialFetchTaskToStart();

            VerifyServiceGetIssues(Times.Exactly(1)); // issues should be fetched on creation

            // Act / Assert - #1 - Longest path is retrieved first
            var matches = issuesProvider.GetSuppressedIssues("", "C:\\AwesomeProject\\src\\bar\\foo\\foo.cs");
            matches.Should().HaveCount(1);

            // Act / Assert - #2 - Shortest path
            matches = issuesProvider.GetSuppressedIssues("", "C:\\AwesomeProject\\src\\bar\\foo.cs");
            matches.Should().HaveCount(1);
        }

        [TestMethod]
        public void GetSuppressedIssues_WhenIssueIsOnAFile_LimitationWhenAttachedToRoot()
        {
            // Arrange
            var sonarQubeIssue1 = new SonarQubeIssue("foo.cs", null, null, "message", "sqkey",
                SonarQubeIssueResolutionState.FalsePositive, "S1");

            SetupSolutionBinding(true,
                new List<SonarQubeIssue> { sonarQubeIssue1 },
                new List<SonarQubeModule> { new SonarQubeModule("sqkey", "", "") });

            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, "sqKey", mockTimerFactory.Object, testLogger);
            WaitForInitialFetchTaskToStart();

            VerifyServiceGetIssues(Times.Exactly(1)); // issues should be fetched on creation

            // Act / Assert - same file name exists at multiple levels in the hierarchy
            var matches = issuesProvider.GetSuppressedIssues("", "C:\\AwesomeProject\\src\\bar\\foo\\foo.cs");
            matches.Should().HaveCount(1);

            matches = issuesProvider.GetSuppressedIssues("", "C:\\AwesomeProject\\src\\bar\\foo.cs");
            matches.Should().HaveCount(1);
        }        

        [TestMethod]
        public void GetIssues_IssuesNotYetFetch_WaitsForIssuesToBeFetched()
        {
            var issue1 = new SonarQubeIssue("folder1/file1", "hash1", 0, "message", "sqkey:sqkey:projectId",
                SonarQubeIssueResolutionState.FalsePositive, "S101");

            int callbackCount = 0;
            bool callbackCompleted = false;
            Func<IList<SonarQubeIssue>> serviceFetchIssuesTask = () =>
            {
                callbackCount++;

                InitialFetchWaitHandle.Set(); // signal so the test can continue

                Thread.Sleep(Debugger.IsAttached ? 5000 : 500);
                callbackCompleted = true;
                return new List<SonarQubeIssue> { issue1 };
            };

            SetupSolutionBinding(isConnected: true, serviceFetchIssuesTask: serviceFetchIssuesTask);

            // 1. Create the issue provider
            // The initial fetch should be triggered, but not yet completed
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, "sqKey",
                mockTimerFactory.Object, testLogger);
            WaitForInitialFetchTaskToStart();

            // 2. Now request the issues - should wait until the issues have been retrieved
            var matches = issuesProvider.GetSuppressedIssues("projectId", "folder1\\file1");

            VerifyServiceGetIssues(Times.Once(), "sqKey");
            callbackCount.Should().Be(1);
            callbackCompleted.Should().BeTrue();
            matches.Count().Should().Be(1);
            CheckExpectedIssueReturned("hash1", matches);

            // 3. Now fetch again - should not wait again
            matches = issuesProvider.GetSuppressedIssues("projectId", "folder1\\file1");

            VerifyServiceGetIssues(Times.Once());
            callbackCount.Should().Be(1);
        }

        [TestMethod]
        public void GetIssues_NotConnected_NoErrors()
        {
            // Arrange
            SetupSolutionBinding(isConnected: false, issues: null);

            int callCount = 0;

            // This time we want the test to pause until IsConnected is called by the inital fetch task
            mockSqService.Setup(x => x.IsConnected)
                .Returns(false)
                .Callback(() => { InitialFetchWaitHandle.Set(); callCount++; }) // signal so the test can continue
                .Verifiable();

            // 1. Initialise the class
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, "sqKey",
                mockTimerFactory.Object, testLogger);

            WaitForInitialFetchTaskToStart();

            // 2. Now dispose. Should stop the background fetching
            int callsBeforeDispose = callCount;
            issuesProvider.Dispose();

            // 3. Now try to fetch - should block until the background task has completed
            var issues = issuesProvider.GetSuppressedIssues("dummy guid", "dummy file path");

            issues.Should().BeEmpty();
            VerifyServiceGetIssues(Times.Never());

            // Timing: increment could have afer dispose called but before the cancellation token was set
            callCount.Should().BeLessOrEqualTo(callsBeforeDispose + 1);

            testLogger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void GetIssues_ErrorInInitialFetchTask_IsSuppressed()
        {
            var issue1 = new SonarQubeIssue("folder1/file1", "hash1", 0, "message", "sqkey:sqkey:projectID1",
                SonarQubeIssueResolutionState.FalsePositive, "S101");

            Func<IList<SonarQubeIssue>> serviceFetchIssuesTask = () =>
            {
                InitialFetchWaitHandle.Set(); // signal so the test can continue
                throw new ApplicationException("dummy error from mock");
            };

            SetupSolutionBinding(isConnected: true, serviceFetchIssuesTask: serviceFetchIssuesTask);

            // 1. Create the issue provider
            // The initial fetch should be triggered, but not yet completed
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, "sqkey",
                mockTimerFactory.Object, testLogger);
            WaitForInitialFetchTaskToStart();

            // 2. Now request the issues - task completes with an error
            var matches = issuesProvider.GetSuppressedIssues("projectid1", "folder1/file1");

            VerifyServiceGetIssues(Times.Once(), "sqkey");
            matches.Should().BeEmpty();

            // 3. Now fetch again - should not wait again, should not error
            matches = issuesProvider.GetSuppressedIssues("folder1/file1", "projectid1");
            matches.Should().BeEmpty();
            VerifyServiceGetIssues(Times.Once());

            testLogger.AssertPartialOutputStrings("Checking for suppressions", "dummy error from mock");
        }

        [TestMethod]
        public void GetIssues_ErrorFetchingOnTimerElapsed_IsSuppressed()
        {
            // Tests that the timer trigger that causes the data to be refetched won't propagate errors
            // if the GetIssues call throws.

            var issue1 = new SonarQubeIssue("C:\\folder1\\file1.cs", "hash1", 0, "message", "sqkey:sqkey:projectId",
                SonarQubeIssueResolutionState.FalsePositive, "S101");
            SetupSolutionBinding(isConnected: true, issues: new List<SonarQubeIssue> { issue1 });

            // 1. Create the issue provider and call GetIssues to make sure the issues are cached
            var issuesProvider = new SonarQubeIssuesProvider(mockSqService.Object, "sqkey",
                mockTimerFactory.Object, testLogger);
            var matches = issuesProvider.GetSuppressedIssues("projectId", "C:\\folder1\\file1.cs");
            VerifyServiceGetIssues(Times.Once());
            matches.Count().Should().Be(1);
            testLogger.AssertPartialOutputStrings("Checking for suppressions", "1");

            // 2. Configure service to throw, then execute the fetch trigger
            int fetchCallCount = 0;
            testLogger.Reset();
            Func<IList<SonarQubeIssue>> serviceFetchIssuesTask = () =>
            {
                fetchCallCount++;
                throw new ApplicationException("dummy error from mock");
            };
            SetupSolutionBinding(isConnected: true, serviceFetchIssuesTask: serviceFetchIssuesTask);

            RaiseTimerElapsed(DateTime.UtcNow);

            // 3. Fetch issues again - should used cached issues
            matches = issuesProvider.GetSuppressedIssues("projectId", "C:\\folder1\\file1.cs");
            VerifyServiceGetIssues(Times.Exactly(2));
            matches.Count().Should().Be(1);
            fetchCallCount.Should().Be(1);

            VerifyTimerStart(Times.Exactly(1)); // once, on construction
            testLogger.AssertPartialOutputStrings("Checking for suppressions", "dummy error from mock");
        }

        #endregion

        /// <summary>
        /// Configures the mock service to return the specified values
        /// </summary>
        private void SetupSolutionBinding(bool isConnected, IList<SonarQubeIssue> issues, IList<SonarQubeModule> modules = null)
        {
            Func<IList<SonarQubeIssue>> serviceFetchIssuesTask = () =>
            {
                InitialFetchWaitHandle?.Set(); // signal so the test can continue
                return issues;
            };

            SetupSolutionBinding(isConnected, serviceFetchIssuesTask, modules);
        }

        /// <summary>
        /// Configures the service to execute the supplied function when
        /// GetSuppressed issues is called
        /// </summary>
        private void SetupSolutionBinding(bool isConnected, Func<IList<SonarQubeIssue>> serviceFetchIssuesTask,
            IList<SonarQubeModule> modules = null)
        {
            // Note: if the solution is set up disconnected then the initial fetch background
            // task will run in a loop - make sure the calling test takes account of this

            mockSqService.Setup(x => x.IsConnected).Returns(isConnected).Verifiable();

            InitialFetchWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            mockSqService.Setup(x => x.GetAllModulesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(modules ?? new List<SonarQubeModule> { new SonarQubeModule("sqkey:sqkey:projectId", "", "") })
                .Verifiable();

            mockSqService.Setup(x => x.GetSuppressedIssuesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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

        private void VerifyServiceGetIssues(Times expected, string sonarQubeProjectKey)
        {
            mockSqService.Verify(x => x.GetSuppressedIssuesAsync(sonarQubeProjectKey, It.IsAny<CancellationToken>()), expected);
        }

        private void VerifyServiceIsConnected(Times expected)
        {
            mockSqService.Verify(x => x.IsConnected, expected);
        }

        private void RaiseTimerElapsed(DateTime eventTime)
        {
            mockTimer.Raise(t => t.Elapsed += null, new TimerEventArgs(eventTime));
        }

        private static void CheckExpectedIssueReturned(string expectedHash, IEnumerable<SonarQubeIssue> actualIssues)
        {
            SonarQubeIssue match = actualIssues.FirstOrDefault(i => i.Hash.Equals(expectedHash, StringComparison.InvariantCultureIgnoreCase));
            match.Should().NotBeNull();
        }
    }
}
