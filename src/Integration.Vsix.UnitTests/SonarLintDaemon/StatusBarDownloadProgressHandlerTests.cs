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

using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintDaemon
{
    [TestClass]
    public class StatusBarDownloadProgressHandlerTests
    {
        private const int ExpectedCookie = 123;

        private DummyStatusBar dummyStatusBar;
        private DummyDaemonInstaller dummyInstaller;
        private TestLogger logger;

        [TestInitialize]
        public void TestInitialize()
        {
            dummyStatusBar = new DummyStatusBar(ExpectedCookie);
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
            CheckProgressParamaters(dummyStatusBar,
                0, // 0 on first call
                1, // "in progress"
                0, 1000);

            // 2. Progress updates
            dummyInstaller.SimulateProgressChanged(new InstallationProgressChangedEventArgs(100, 1000));
            CheckProgressParamaters(dummyStatusBar,
                ExpectedCookie, // should not be using the cookie
                1, // "in progress"
                100, 1000);


            // 3. Cleanup - reset the statusbar
            dummyInstaller.SimulateInstallFinished(new System.ComponentModel.AsyncCompletedEventArgs(null, false, null));
            CheckProgressParamaters(dummyStatusBar,
                ExpectedCookie, // should still be using the cookie
                0, // "finished"
                0, 0);

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
            CheckProgressParamaters(dummyStatusBar,
                0, // 0 on first call
                1, // "in progress"
                0, 1000);

            // 2. Progress updates
            dummyInstaller.SimulateProgressChanged(new InstallationProgressChangedEventArgs(100, 1000));
            CheckProgressParamaters(dummyStatusBar,
                ExpectedCookie, // should not be using the cookie
                1, // "in progress"
                100, 1000);

            dummyStatusBar.ProgressCallCount.Should().Be(2);

            // 3. Dispose - unhook event handlers, then simulate more events
            progressHandler.Dispose();
            dummyInstaller.AssertNoEventHandlersRegistered();
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
        }

        private static void CheckProgressParamaters(DummyStatusBar dummyStatusBar, uint expectedCookie, int expectedProgress, uint expectedNComplete, uint expectedNTotal)
        {
            dummyStatusBar.LastPwdCookie.Should().Be(expectedCookie);
            dummyStatusBar.LastfInProgress.Should().Be(expectedProgress);
            dummyStatusBar.LastnComplete.Should().Be(expectedNComplete);
            dummyStatusBar.LastnTotal.Should().Be(expectedNTotal);
        }

        private class DummyStatusBar : IVsStatusbar
        {
            private uint cookieToReturn;

            public DummyStatusBar(uint cookieToReturn)
            {
                this.cookieToReturn = cookieToReturn;
            }

            public int ProgressCallCount { get; set; }

            public Action ProgressOperation { get; set; }

            #region IVsStatusbar methods

            public int Clear()
            {
                throw new NotImplementedException();
            }

            public int SetText(string pszText)
            {
                throw new NotImplementedException();
            }

            public uint LastPwdCookie { get; private set; }
            public int LastfInProgress { get; private set; }
            public string LastLabel { get; private set; }
            public uint LastnComplete { get; private set; }
            public uint LastnTotal { get; private set; }


            public int Progress(ref uint pdwCookie, int fInProgress, string pwszLabel, uint nComplete, uint nTotal)
            {
                ProgressCallCount++;

                LastPwdCookie = pdwCookie;
                LastfInProgress = fInProgress;
                LastLabel = pwszLabel;
                LastnComplete = nComplete;
                LastnTotal = nTotal;

                if (pdwCookie == 0)
                {
                    pdwCookie = cookieToReturn;
                }

                ProgressOperation?.Invoke();

                return 0; // success
            }

            public int Animation(int fOnOff, ref object pvIcon)
            {
                throw new NotImplementedException();
            }

            public int SetSelMode(ref object pvSelMode)
            {
                throw new NotImplementedException();
            }

            public int SetInsMode(ref object pvInsMode)
            {
                throw new NotImplementedException();
            }

            public int SetLineChar(ref object pvLine, ref object pvChar)
            {
                throw new NotImplementedException();
            }

            public int SetXYWH(ref object pvX, ref object pvY, ref object pvW, ref object pvH)
            {
                throw new NotImplementedException();
            }

            public int SetLineColChar(ref object pvLine, ref object pvCol, ref object pvChar)
            {
                throw new NotImplementedException();
            }

            public int IsCurrentUser(IVsStatusbarUser pUser, ref int pfCurrent)
            {
                throw new NotImplementedException();
            }

            public int SetColorText(string pszText, uint crForeColor, uint crBackColor)
            {
                throw new NotImplementedException();
            }

            public int GetText(out string pszText)
            {
                throw new NotImplementedException();
            }

            public int FreezeOutput(int fFreeze)
            {
                throw new NotImplementedException();
            }

            public int IsFrozen(out int pfFrozen)
            {
                throw new NotImplementedException();
            }

            public int GetFreezeCount(out int plCount)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

    }
}
