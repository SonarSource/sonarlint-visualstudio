﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger
{
    [TestClass]
    public class CancellableJobRunnerTests
    {
        [TestMethod]
        public void NoOperations_NoErrors()
        {
            // Arrange
            var testLogger = new TestLogger(logToConsole: true, logThreadId: true);
            testLogger.WriteLine("[Test] Executing test");
            var progressRecorder = new ProgressNotificationRecorder(testLogger);

            // Act
            var testSubject = CancellableJobRunner.Start("my job", new Action[] { }, progressRecorder, testLogger);
            WaitForRunnerToFinish(testSubject, testLogger);

            // Assert
            testSubject.State.Should().Be(CancellableJobRunner.RunnerState.Finished);

            progressRecorder.Notifications.Count.Should().Be(2);
            CheckExpectedNotification(progressRecorder.Notifications[0], CancellableJobRunner.RunnerState.Running, 0, 0);
            CheckExpectedNotification(progressRecorder.Notifications[1], CancellableJobRunner.RunnerState.Finished, 0, 0);
        }

        [TestMethod]
        public void NoOperations_NoProgressHandler_NoErrors()
        {
            // Arrange
            var testLogger = new TestLogger(logToConsole: true, logThreadId: true);
            testLogger.WriteLine("[Test] Executing test");

            // Act
            var testSubject = CancellableJobRunner.Start("my job", new Action[] { },
                null /* should be ok to pass null here */,
                testLogger);
            WaitForRunnerToFinish(testSubject, testLogger);

            // Assert
            testSubject.State.Should().Be(CancellableJobRunner.RunnerState.Finished);
        }

        [TestMethod]
        public void AllOperationsExecuted()
        {
            // Arrange
            var testLogger = new TestLogger(logToConsole: true, logThreadId: true);
            testLogger.WriteLine("[Test] Executing test");

            var progressRecorder = new ProgressNotificationRecorder(testLogger);

            bool op1Executed = false, op2Executed = false;
            int operationThreadId = -1;
            
            CancellableJobRunner testSubject = null;

            Action op1 = () =>
            {
                testLogger.WriteLine("[Test] Executing op1");
                testSubject.State.Should().Be(CancellableJobRunner.RunnerState.Running);

                op1Executed = true;
                operationThreadId = Thread.CurrentThread.ManagedThreadId;
            };

            Action op2 = () => op2Executed = true;

            // Act
            testSubject = CancellableJobRunner.Start("my job", new[] { op1, op2 }, progressRecorder, testLogger);
            WaitForRunnerToFinish(testSubject, testLogger);
            
            // Assert
            testSubject.State.Should().Be(CancellableJobRunner.RunnerState.Finished);

            op1Executed.Should().BeTrue();
            op2Executed.Should().BeTrue();

            operationThreadId.Should().NotBe(Thread.CurrentThread.ManagedThreadId);

            progressRecorder.Notifications.Count.Should().Be(4);
            CheckExpectedNotification(progressRecorder.Notifications[0], CancellableJobRunner.RunnerState.Running, 0, 2);
            CheckExpectedNotification(progressRecorder.Notifications[1], CancellableJobRunner.RunnerState.Running, 1, 2);
            CheckExpectedNotification(progressRecorder.Notifications[2], CancellableJobRunner.RunnerState.Running, 2, 2);
            CheckExpectedNotification(progressRecorder.Notifications[3], CancellableJobRunner.RunnerState.Finished, 2, 2);
        }

        [TestMethod]
        public void CancelAfterFirstOperation()
        {
            // Arrange
            var testLogger = new TestLogger(logToConsole: true, logThreadId: true);
            testLogger.WriteLine("[Test] Executing test");
            var progressRecorder = new ProgressNotificationRecorder(testLogger);

            bool op1Executed = false, op2Executed = false;
            CancellableJobRunner testSubject = null;

            Action op1 = () =>
            {
                testLogger.WriteLine("[Test] Executing op1");
                op1Executed = true;

                testSubject.Cancel();
            };

            Action op2 = () =>
            {
                testLogger.WriteLine("[Test] Executing op2");
                op2Executed = true;
            };

            // Act
            testSubject = CancellableJobRunner.Start("my job", new[] { op1, op2 }, progressRecorder, testLogger);


            WaitForRunnerToFinish(testSubject, testLogger);
            // Pause for any final progress steps to be reported before checking the progressRecorder below
            Thread.Sleep(200);

            // Other checks
            testSubject.State.Should().Be(CancellableJobRunner.RunnerState.Cancelled);
            op1Executed.Should().BeTrue();
            op2Executed.Should().BeFalse();

            progressRecorder.Notifications.Count.Should().Be(2);
            CheckExpectedNotification(progressRecorder.Notifications[0], CancellableJobRunner.RunnerState.Running, 0, 2);
            CheckExpectedNotification(progressRecorder.Notifications[1], CancellableJobRunner.RunnerState.Cancelled, 0, 2);
        }

        [TestMethod]
        public void ExceptionInOperationPreventsSubsequentOperations()
        {
            // Arrange
            var testLogger = new TestLogger(logToConsole: true, logThreadId: true);
            testLogger.WriteLine("[Test] Executing test");
            var progressRecorder = new ProgressNotificationRecorder(testLogger);

            bool op1Executed = false, op2Executed = false;

            Action op1 = () =>
            {
                testLogger.WriteLine("[Test] Executing op1");
                op1Executed = true;
                throw new InvalidOperationException("XXX YYY");
            };

            Action op2 = () =>
            {
                testLogger.WriteLine("[Test] Executing op2");
                op2Executed = true;
            };

            // Act
            var testSubject = CancellableJobRunner.Start("my job", new[] { op1, op2 }, progressRecorder, testLogger);
            WaitForRunnerToFinish(testSubject, testLogger);

            // Other checks
            testSubject.State.Should().Be(CancellableJobRunner.RunnerState.Faulted);
            testLogger.AssertPartialOutputStringExists("XXX YYY");

            op1Executed.Should().BeTrue();
            op2Executed.Should().BeFalse();

            progressRecorder.Notifications.Count.Should().Be(2);
            CheckExpectedNotification(progressRecorder.Notifications[0], CancellableJobRunner.RunnerState.Running, 0, 2);
            CheckExpectedNotification(progressRecorder.Notifications[1], CancellableJobRunner.RunnerState.Faulted, 0, 2);
        }

        private static void WaitForRunnerToFinish(CancellableJobRunner runner, ILogger logger)
        {
            int timeout = System.Diagnostics.Debugger.IsAttached ? 20000 : 3000;
            bool signalled = false;

            try
            {
                signalled = runner.TestingWaitHandle?.WaitOne(timeout) ?? false;
                if (signalled)
                {
                    logger.WriteLine("[Test] In WaitForRunnerToFinish. Event was signalled.");
                }
                else
                {
                    logger.WriteLine("[Test] In WaitForRunnerToFinish: Event was not signalled.");
                }
            }
            catch (ObjectDisposedException)
            {
                // If the runner has finished then the token source will have been disposed
                logger.WriteLine("[Test] In WaitForRunnerToFinish: Caught ObjectDisposedException.");
            }
        }

        private static void CheckExpectedNotification(CancellableJobRunner.JobRunnerProgress actual,
            CancellableJobRunner.RunnerState expectedState, int expectedCompleted, int expectedTotal)
        {
            actual.CurrentState.Should().Be(expectedState);
            actual.CompletedOperations.Should().Be(expectedCompleted);
            actual.TotalOperations.Should().Be(expectedTotal);
        }

        private class ProgressNotificationRecorder : IProgress<CancellableJobRunner.JobRunnerProgress>
        {
            private readonly ILogger logger;
            public ProgressNotificationRecorder(ILogger logger)
            {
                this.logger = logger;
            }

            public IList<CancellableJobRunner.JobRunnerProgress> Notifications { get; } = new List<CancellableJobRunner.JobRunnerProgress>();

            public void Report(CancellableJobRunner.JobRunnerProgress value)
            {
                logger.WriteLine($"[Test] In progress reporter. State: {value.CurrentState}");
                Notifications.Add(value);
            }
        }
    }
}
