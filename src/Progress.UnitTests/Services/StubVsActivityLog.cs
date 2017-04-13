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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation for <see cref="IVsActivityLog"/>
    /// </summary>
    public class StubVsActivityLog : IVsActivityLog
    {
        internal bool IsEntryLogged { get; private set; } = false;

        #region Configuration

        public Action<uint, string, string> LogEntryAction
        {
            get;
            set;
        }

        #endregion Configuration

        #region Test helpers

        public void Reset()
        {
            this.IsEntryLogged = false;
        }

        #endregion Test helpers

        #region IVsActivityLog

        int IVsActivityLog.LogEntry(uint actType, string pszSource, string pszDescription)
        {
            this.IsEntryLogged = true;
            this.LogEntryAction?.Invoke(actType, pszSource, pszDescription);

            return VSConstants.S_OK;
        }

        int IVsActivityLog.LogEntryGuid(uint actType, string pszSource, string pszDescription, Guid guid)
        {
            throw new NotImplementedException();
        }

        int IVsActivityLog.LogEntryGuidHr(uint actType, string pszSource, string pszDescription, Guid guid, int hr)
        {
            throw new NotImplementedException();
        }

        int IVsActivityLog.LogEntryGuidHrPath(uint actType, string pszSource, string pszDescription, Guid guid, int hr, string pszPath)
        {
            throw new NotImplementedException();
        }

        int IVsActivityLog.LogEntryGuidPath(uint actType, string pszSource, string pszDescription, Guid guid, string pszPath)
        {
            throw new NotImplementedException();
        }

        int IVsActivityLog.LogEntryHr(uint actType, string pszSource, string pszDescription, int hr)
        {
            throw new NotImplementedException();
        }

        int IVsActivityLog.LogEntryHrPath(uint actType, string pszSource, string pszDescription, int hr, string pszPath)
        {
            throw new NotImplementedException();
        }

        int IVsActivityLog.LogEntryPath(uint actType, string pszSource, string pszDescription, string pszPath)
        {
            throw new NotImplementedException();
        }

        #endregion IVsActivityLog
    }
}