//-----------------------------------------------------------------------
// <copyright file="StubVsActivityLog.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation for <see cref="IVsActivityLog"/>
    /// </summary>
    public class StubVsActivityLog : IVsActivityLog
    {
        private bool loggedEntry = false;

        #region Configuration
        public Action<uint, string, string> LogEntryAction
        {
            get;
            set;
        }
        #endregion

        #region Test helpers
        public void Reset()
        {
            this.loggedEntry = false;
        }
        #endregion

        #region Verification
        public void AssertEntryLogged()
        {
            Assert.IsTrue(this.loggedEntry, "No requests to log entry to activity log");
        }

        public void AssertEntryNotLogged()
        {
            Assert.IsFalse(this.loggedEntry, "Not expected any requests to log to activity log");
        }

        #endregion

        #region IVsActivityLog
        int IVsActivityLog.LogEntry(uint actType, string pszSource, string pszDescription)
        {
            this.loggedEntry = true;
            if (this.LogEntryAction != null)
            {
                this.LogEntryAction(actType, pszSource, pszDescription);
            }

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
        #endregion
    }
}
