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
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableVsStatusbar : IVsStatusbar
        {
            private uint cookieToReturn;

            public ConfigurableVsStatusbar(uint cookieToReturn)
            {
                this.cookieToReturn = cookieToReturn;
            }

            public int ProgressCallCount { get; set; }

            public Action ProgressOperation { get; set; }

            #region Checks

            public void CheckLastCallWasSetupCall(uint expectedNComplete, uint expectedNTotal)
            {
                LastPwdCookie.Should().Be(0); // caller should have supplied 0 for the first call
                LastfInProgress.Should().Be(1); // should be 1 for all calls except "finished"
                LastnComplete.Should().Be(expectedNComplete);
                LastnTotal.Should().Be(expectedNTotal);
            }

            public void CheckLastCallWasInProgressCall(uint expectedNComplete, uint expectedNTotal)
            {
                LastPwdCookie.Should().Be(cookieToReturn); // in-progress call should use the status bar cookie
                LastfInProgress.Should().Be(1);  // should be 1 for all calls except "finished"
                LastnComplete.Should().Be(expectedNComplete);
                LastnTotal.Should().Be(expectedNTotal);
            }

            public void CheckLastCallWasCleanup()
            {
                LastPwdCookie.Should().Be(cookieToReturn); // cleanup call should use the status bar cookie
                LastfInProgress.Should().Be(0); // "finished"
                LastnComplete.Should().Be(0); // completed and ignored progress arguments are ignored during cleanup
                LastnTotal.Should().Be(0);
            }

            #endregion

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
