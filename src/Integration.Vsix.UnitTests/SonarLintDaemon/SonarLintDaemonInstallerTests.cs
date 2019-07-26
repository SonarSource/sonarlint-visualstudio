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
using System.ComponentModel;
using System.Net;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using static SonarLint.VisualStudio.Integration.Vsix.SonarLintDaemonInstaller;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintDaemon
{
    [TestClass]
    public class SonarLintDaemonInstallerTests
    {
        [TestMethod]
        public void Ctor_WithNullSettings_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarLintDaemonInstaller(null, new Mock<ISonarLintDaemon>().Object, new TestLogger());

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("settings");
        }

        [TestMethod]
        public void Ctor_WithNullDaemon_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarLintDaemonInstaller(new Mock<ISonarLintSettings>().Object, null, new TestLogger());

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("daemon");
        }

        [TestMethod]
        public void Ctor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarLintDaemonInstaller(new Mock<ISonarLintSettings>().Object, new Mock<ISonarLintDaemon>().Object, null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void InstallationStarted_NoErrors_EventHandlersRegistered()
        {
            // Arrange
            var dummyDaemon = new DummyDaemon();
            var testLogger = new TestLogger();

            var installer = new SonarLintDaemonInstaller(new Mock<ISonarLintSettings>().Object, dummyDaemon, testLogger);
            bool installCalled = false;
            dummyDaemon.DaemonInstallOperation = () => installCalled = true;

            // Act
            installer.SafeBeginInstallation();

            // Assert
            dummyDaemon.IsDownloadCompletedEventHandled().Should().BeTrue();
            dummyDaemon.IsDownloadProgressChangedEventHandled().Should().BeTrue();
            installCalled.Should().BeTrue();
            dummyDaemon.WasStartCalled.Should().BeFalse();
        }

        [TestMethod]
        public void InstallationStarted_NonCriticalException_Suppressed()
        {
            // Arrange
            var dummyDaemon = new DummyDaemon();
            var testLogger = new TestLogger();

            dummyDaemon.DaemonInstallOperation = () => throw new InvalidOperationException("XXX dummy exception");

            var installer = new SonarLintDaemonInstaller(new Mock<ISonarLintSettings>().Object, dummyDaemon, testLogger);

            // Act
            installer.SafeBeginInstallation();
            
            // Assert
            testLogger.AssertPartialOutputStringExists("XXX dummy exception");
            dummyDaemon.WasStartCalled.Should().BeFalse();
        }

        [TestMethod]
        public void InstallationStarted_CriticalException_NotSuppressed()
        {
            // Arrange
            var dummyDaemon = new DummyDaemon();
            var testLogger = new TestLogger();

            dummyDaemon.DaemonInstallOperation = () => throw new StackOverflowException("XXX dummy exception");

            var installer = new SonarLintDaemonInstaller(new Mock<ISonarLintSettings>().Object, dummyDaemon, testLogger);

            // Act
            Action act = () => installer.SafeBeginInstallation();

            // Assert
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("XXX dummy exception");
        }

        [TestMethod]
        public void DownloadCompleted_DialogNotClosed_DaemonStarted()
        {
            // Arrange
            var dummyDaemon = new DummyDaemon();
            var testLogger = new TestLogger();

            bool errorMessageDisplayed = false;
            DisplayMessageBoxDelegate displayMessageBox = (m, t) => { errorMessageDisplayed = true; };

            var installer = new SonarLintDaemonInstaller(new Mock<ISonarLintSettings>().Object, dummyDaemon, testLogger);
            var args = new AsyncCompletedEventArgs(null, false, null);

            // Act - simulate download ending
            installer.SafeHandleDownloadCompleted(args, displayMessageBox);

            // Assert
            dummyDaemon.WasStartCalled.Should().BeTrue();
            errorMessageDisplayed.Should().BeFalse();
        }

        [TestMethod]
        public void DownloadCompletedEvent_DialogClosed_DaemonNotStarted()
        {
            // Arrange
            var dummyDaemon = new DummyDaemon();
            var testLogger = new TestLogger();

            bool errorMessageDisplayed = false;
            DisplayMessageBoxDelegate displayMessageBox = (m, t) => { errorMessageDisplayed = true; };

            var installer = new SonarLintDaemonInstaller(new Mock<ISonarLintSettings>().Object, dummyDaemon, testLogger);

            var args = new AsyncCompletedEventArgs(null, false, null); // Note: the cancellation property of event args is ignored

            // 1. Simulate the user closing the dialogue before the download completes
            installer.Close();

            // 2. Simulate download finished
            installer.SafeHandleDownloadCompleted(args, displayMessageBox);

            // Assert
            dummyDaemon.WasStartCalled.Should().BeFalse();
            errorMessageDisplayed.Should().BeFalse();
        }

        [TestMethod]
        public void DownloadCompleted_ErrorInDownloadEvent_Logged()
        {
            // Arrange
            var dummyDaemon = new DummyDaemon();
            var testLogger = new TestLogger();

            string errorTitle = null;
            string errorMessage = null;
            DisplayMessageBoxDelegate displayMessageBox = (m, t) => { errorMessage = m; errorTitle = t; };

            var installer = new SonarLintDaemonInstaller(new Mock<ISonarLintSettings>().Object, dummyDaemon, testLogger);

            var args = new AsyncCompletedEventArgs(new ArgumentException("XXX download error"), false, null);

            // Act - simulate download finished with an error
            installer.SafeHandleDownloadCompleted(args, displayMessageBox);

            // Assert
            dummyDaemon.WasStartCalled.Should().BeFalse(); // don't start in event of error

            testLogger.AssertPartialOutputStringExists("XXX download error");
            errorMessage.Should().Contain("XXX download error");
            errorTitle.Should().Be(Strings.Daemon_Download_ErrorDlgTitle);
        }

        [TestMethod]
        public void DownloadCompletedEvent_NonCriticalExceptionThrown_Suppressed()
        {
            // Arrange
            var dummyDaemon = new DummyDaemon();
            var testLogger = new TestLogger();

            dummyDaemon.DaemonStartedOperation = () => throw new InvalidOperationException("XXX dummy exception");

            var installer = new SonarLintDaemonInstaller(new Mock<ISonarLintSettings>().Object, dummyDaemon, testLogger);
            var args = new AsyncCompletedEventArgs(null, false, null);
            Action act = () => dummyDaemon.RaiseDownloadCompleteEvent(args);

            // Act
            installer.SafeHandleDownloadCompleted(args, null);

            // Assert
            dummyDaemon.WasStartCalled.Should().BeTrue();
            testLogger.AssertPartialOutputStringExists("XXX dummy exception");
        }

        [TestMethod]
        public void DownloadCompletedEvent_CriticalExceptionThrown_NotSuppressed()
        {
            // Arrange
            var dummyDaemon = new DummyDaemon();
            var testLogger = new TestLogger();

            dummyDaemon.DaemonStartedOperation = () => throw new StackOverflowException("XXX dummy exception");

            var installer = new SonarLintDaemonInstaller(new Mock<ISonarLintSettings>().Object, dummyDaemon, testLogger);
            var args = new AsyncCompletedEventArgs(null, false, null);
            Action act = () => dummyDaemon.RaiseDownloadCompleteEvent(args);

            // Act - simulate download beginning
            installer.SafeBeginInstallation();

            // Assert
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("XXX dummy exception");
        }

        private class DummyDaemon : ISonarLintDaemon
        {
            public void RaiseDownloadCompleteEvent(AsyncCompletedEventArgs args)
            {
                this.DownloadCompleted(this, args);
            }

            public bool IsDownloadProgressChangedEventHandled()
                => DownloadProgressChanged != null;

            public bool IsDownloadCompletedEventHandled()
                => DownloadCompleted != null;

            public bool WasStartCalled { get; private set; }

            public Action DaemonInstallOperation { get; set; }

            public Action DaemonStartedOperation { get; set; }

            #region ISonarLintDaemon interface

            public bool IsInstalled { get; set; }

            public bool IsRunning { get; set; }

            public event DownloadProgressChangedEventHandler DownloadProgressChanged;
            public event AsyncCompletedEventHandler DownloadCompleted;
            public event EventHandler<EventArgs> Ready;

            public void Dispose()
            {
                // no-op
            }

            public void Install()
            {
                DaemonInstallOperation?.Invoke();
            }

            public void RequestAnalysis(string path, string charset, string sqLanguage, IIssueConsumer consumer)
            {
                throw new NotImplementedException();
            }

            public void Start()
            {
                WasStartCalled = true;
                DaemonStartedOperation?.Invoke();
            }

            public void Stop()
            {
                throw new NotImplementedException();
            }

            #endregion ISonarLintDaemon interface
        }
    }
}
