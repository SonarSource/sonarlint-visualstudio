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
        [TestMethod]
        public void LifeCycle()
        {
            // Arrange
            var dummyStatusBar = new DummyStatusBar(123);
            var dummyInstaller = new DummyDaemonInstaller();
            var logger = new TestLogger();

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
                123, // should not be using the cookie
                1, // "in progress"
                100, 1000);


            // 3. Cleanup - reset the statusbar
            dummyInstaller.SimulateInstallFinished(new System.ComponentModel.AsyncCompletedEventArgs(null, false, null));
            CheckProgressParamaters(dummyStatusBar,
                123, // should still be using the cookie
                0, // "finished"
                0, 0);

            progressHandler = null;
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
                LastPwdCookie = pdwCookie;
                LastfInProgress = fInProgress;
                LastLabel = pwszLabel;
                LastnComplete = nComplete;
                LastnTotal = nTotal;

                if (pdwCookie == 0)
                {
                    pdwCookie = cookieToReturn;
                }

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
