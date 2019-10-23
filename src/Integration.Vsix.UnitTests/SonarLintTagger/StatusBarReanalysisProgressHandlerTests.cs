/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class StatusBarReanalysisProgressHandlerTests
    {
        private ConfigurableVsStatusbar dummyStatusBar;
        private TestLogger logger;

        [TestInitialize]
        public void TestInitialize()
        {
            dummyStatusBar = new ConfigurableVsStatusbar(123);
            logger = new TestLogger();
        }

        [TestMethod]
        public void LifeCycle()
        {
            // Arrange
            var progressHandler = new StatusBarReanalysisProgressHandler(dummyStatusBar, logger);

            // 1. Initial request
            ReportProgress(progressHandler, CancellableJobRunner.RunnerState.Running, 0, 2);
            dummyStatusBar.CheckLastCallWasSetupCall(0, 2);

            // 2a. Progress updates
            ReportProgress(progressHandler, CancellableJobRunner.RunnerState.Running, 1, 2);
            dummyStatusBar.CheckLastCallWasInProgressCall(1, 2);

            // 2b. Progress updates
            ReportProgress(progressHandler, CancellableJobRunner.RunnerState.Running, 2, 2);
            dummyStatusBar.CheckLastCallWasInProgressCall(2, 2);

            // 3. Finished -> clean up and reset the statusbar
            ReportProgress(progressHandler, CancellableJobRunner.RunnerState.Finished, 111, 222);

            dummyStatusBar.CheckLastCallWasCleanup();
            dummyStatusBar.ProgressCallCount.Should().Be(4);

            // 4. Dispose - should be a no-op in this case
            progressHandler.Dispose();
            dummyStatusBar.ProgressCallCount.Should().Be(4); // status bar should not be called since already cleaned up
        }

        [TestMethod]
        public void LifeCycle_Dispose_ClearsStatusBar()
        {
            // Arrange
            var progressHandler = new StatusBarReanalysisProgressHandler(dummyStatusBar, logger);

            // 1. Initial request
            ReportProgress(progressHandler, CancellableJobRunner.RunnerState.Running, 0, 1000);
            dummyStatusBar.CheckLastCallWasSetupCall(0, 1000);

            // 2. Progress updates
            ReportProgress(progressHandler, CancellableJobRunner.RunnerState.Running, 100, 1000);
            dummyStatusBar.CheckLastCallWasInProgressCall(100, 1000);

            dummyStatusBar.ProgressCallCount.Should().Be(2);

            // 3. Dispose
            progressHandler.Dispose();

            dummyStatusBar.CheckLastCallWasCleanup();
            dummyStatusBar.ProgressCallCount.Should().Be(3);

            // 4. Any further notifications should be ignored
            ReportProgress(progressHandler, CancellableJobRunner.RunnerState.Running, 111, 222);
            ReportProgress(progressHandler, CancellableJobRunner.RunnerState.Faulted, 333, 444);
            ReportProgress(progressHandler, CancellableJobRunner.RunnerState.Finished, 555, 666);
            dummyStatusBar.ProgressCallCount.Should().Be(3); // no further calls
        }

        [TestMethod]
        public void LifeCycle_ProgressStateFaulted_ClearsStatusBar()
        {
            // Arrange
            var progressHandler = new StatusBarReanalysisProgressHandler(dummyStatusBar, logger);

            // 1. Initial request
            ReportProgress(progressHandler, CancellableJobRunner.RunnerState.Running, 0, 1000);
            dummyStatusBar.CheckLastCallWasSetupCall(0, 1000);

            // 2. Progress updates
            ReportProgress(progressHandler, CancellableJobRunner.RunnerState.Running, 100, 1000);
            dummyStatusBar.CheckLastCallWasInProgressCall(100, 1000);

            dummyStatusBar.ProgressCallCount.Should().Be(2);

            // 3. Report a fault -  should cause statusbar cleanup
            ReportProgress(progressHandler, CancellableJobRunner.RunnerState.Faulted, 100, 1000);

            dummyStatusBar.CheckLastCallWasCleanup();
            dummyStatusBar.ProgressCallCount.Should().Be(3);

            // 4. Any further notifications should be ignored
            ReportProgress(progressHandler, CancellableJobRunner.RunnerState.Running, 111, 222);
            ReportProgress(progressHandler, CancellableJobRunner.RunnerState.Finished, 555, 666);
            dummyStatusBar.ProgressCallCount.Should().Be(3); // no further calls
        }

        [TestMethod]
        public void LifeCycle_ProgressChanged_NonCriticalException_Suppressed()
        {
            // Arrange
            bool opExecuted = false;
            dummyStatusBar.ProgressOperation = () =>
            {
                opExecuted = true;
                throw new InvalidOperationException("xxx");
            };

            var progressHandler = new StatusBarReanalysisProgressHandler(dummyStatusBar, logger);

            // Act and Assert: exception should be suppressed
            ReportProgress(progressHandler, CancellableJobRunner.RunnerState.Running, 0, 1000);
            opExecuted.Should().BeTrue();

            logger.AssertPartialOutputStringExists("xxx");
        }

        [TestMethod]
        public void LifeCycle_ProgressChanged_CriticalException_NotSuppressed()
        {
            // Arrange
            dummyStatusBar.ProgressOperation = () => throw new StackOverflowException("xxx");

            var progressHandler = new StatusBarReanalysisProgressHandler(dummyStatusBar, logger);

            // Act and Assert: exception should not be suppressed
            dummyStatusBar.ProgressOperation = () => throw new StackOverflowException("xxx");

            Action act = () => ReportProgress(progressHandler, CancellableJobRunner.RunnerState.Running, 1, 2);
            act.Should().Throw<StackOverflowException>().And.Message.Should().Be("xxx");

            logger.AssertPartialOutputStringDoesNotExist("xxx");
        }

        private void ReportProgress(StatusBarReanalysisProgressHandler handler, CancellableJobRunner.RunnerState runnerState, int completedOperations, int totalOperations)
        {
            handler.Report(new CancellableJobRunner.JobRunnerProgress(runnerState, completedOperations, totalOperations));
        }
    }
}
