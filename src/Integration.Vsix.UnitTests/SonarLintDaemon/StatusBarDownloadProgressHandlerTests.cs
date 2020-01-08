/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintDaemon
{
    [TestClass]
    public class StatusBarDownloadProgressHandlerTests
    {
        private ConfigurableVsStatusbar dummyStatusBar;
        private DummyDaemonInstaller dummyInstaller;
        private TestLogger logger;

        [TestInitialize]
        public void TestInitialize()
        {
            dummyStatusBar = new ConfigurableVsStatusbar(987);
            dummyInstaller = new DummyDaemonInstaller();
            logger = new TestLogger();
        }

        [TestMethod]
        public void LifeCycle()
        {
            // Arrange
            var progressHandler = new StatusBarDownloadProgressHandler(dummyStatusBar, dummyInstaller, logger);

            // 1. Initial request
            dummyInstaller.SimulateProgressChanged(new InstallationProgressChangedEventArgs(0, 1000));
            dummyStatusBar.CheckLastCallWasSetupCall(0, 1000);

            // 2. Progress updates
            dummyInstaller.SimulateProgressChanged(new InstallationProgressChangedEventArgs(100, 1000));
            dummyStatusBar.CheckLastCallWasInProgressCall(100, 1000);


            // 3. Cleanup - reset the statusbar
            dummyInstaller.SimulateInstallFinished(new System.ComponentModel.AsyncCompletedEventArgs(null, false, null));
            dummyStatusBar.CheckLastCallWasCleanup();

            dummyStatusBar.ProgressCallCount.Should().Be(3);
            dummyInstaller.AssertNoEventHandlersRegistered();

            // 4. Dispose
            progressHandler.Dispose();
            dummyStatusBar.ProgressCallCount.Should().Be(3); // progress should not be called since already cleaned up
            dummyInstaller.AssertNoEventHandlersRegistered();
        }

        [TestMethod]
        public void LifeCycle_Dispose_UnhooksEventHandlers()
        {
            // Arrange
            var progressHandler = new StatusBarDownloadProgressHandler(dummyStatusBar, dummyInstaller, logger);

            // 1. Initial request
            dummyInstaller.SimulateProgressChanged(new InstallationProgressChangedEventArgs(0, 1000));
            dummyStatusBar.CheckLastCallWasSetupCall(0, 1000);

            // 2. Progress updates
            dummyInstaller.SimulateProgressChanged(new InstallationProgressChangedEventArgs(100, 1000));
            dummyStatusBar.CheckLastCallWasInProgressCall(100, 1000);

            dummyStatusBar.ProgressCallCount.Should().Be(2);

            // 3. Dispose - unhook event handlers, then simulate more events
            progressHandler.Dispose();

            dummyInstaller.AssertNoEventHandlersRegistered();
            dummyStatusBar.CheckLastCallWasCleanup();
            dummyStatusBar.ProgressCallCount.Should().Be(3);
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

            var progressHandler = new StatusBarDownloadProgressHandler(dummyStatusBar, dummyInstaller, logger);

            // Act and Assert: exception should be suppressed
            dummyInstaller.SimulateProgressChanged(new InstallationProgressChangedEventArgs(0, 1000));
            opExecuted.Should().BeTrue();

            logger.AssertPartialOutputStringExists("xxx");
        }

        [TestMethod]
        public void LifeCycle_ProgressChanged_CriticalException_NotSuppressed()
        {
            // Arrange
            dummyStatusBar.ProgressOperation = () => throw new StackOverflowException("xxx");

            var progressHandler = new StatusBarDownloadProgressHandler(dummyStatusBar, dummyInstaller, logger);

            // Act and Assert: exception should be suppressed
            Action act = () => dummyInstaller.SimulateProgressChanged(new InstallationProgressChangedEventArgs(0, 1000));

            act.Should().Throw<StackOverflowException>().And.Message.Should().Be("xxx");
            logger.AssertPartialOutputStringDoesNotExist("xxx");
        }

        [TestMethod]
        public void LifeCycle_InstallationCompleted_NonCriticalException_Suppressed()
        {
            // Arrange
            bool opExecuted = false;

            var progressHandler = new StatusBarDownloadProgressHandler(dummyStatusBar, dummyInstaller, logger);
            // Initialize the status bar
            dummyInstaller.SimulateProgressChanged(new InstallationProgressChangedEventArgs(0, 100));

            dummyStatusBar.ProgressOperation = () =>
            {
                opExecuted = true;
                throw new InvalidOperationException("xxx");
            };

            // Sanity check
            opExecuted.Should().BeFalse();

            // Act and Assert: exception should be suppressed
            dummyInstaller.SimulateInstallFinished(new System.ComponentModel.AsyncCompletedEventArgs(null, false, null));
            opExecuted.Should().BeTrue();
            logger.AssertPartialOutputStringExists("xxx");
        }

        [TestMethod]
        public void LifeCycle_InstallationCompleted_CriticalException_NotSuppressed()
        {
            // Arrange
            var progressHandler = new StatusBarDownloadProgressHandler(dummyStatusBar, dummyInstaller, logger);
            // Initialize the status bar
            dummyInstaller.SimulateProgressChanged(new InstallationProgressChangedEventArgs(0, 100));

            // Throw an exception next time the progress operation is called
            dummyStatusBar.ProgressOperation = () => throw new StackOverflowException("xxx");

            // Act and Assert: exception should be suppressed
            Action act = () => dummyInstaller.SimulateInstallFinished(new System.ComponentModel.AsyncCompletedEventArgs(null, true, null));

            act.Should().Throw<StackOverflowException>().And.Message.Should().Be("xxx");
            logger.AssertPartialOutputStringDoesNotExist("xxx");
        }
    }
}
