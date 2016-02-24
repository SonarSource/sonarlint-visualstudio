//-----------------------------------------------------------------------
// <copyright file="ConfigurableVsActivityLog.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsActivityLog : IVsActivityLog
    {
        #region IVsActivityLog
        int IVsActivityLog.LogEntry(uint actType, string pszSource, string pszDescription)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        int IVsActivityLog.LogEntryGuid(uint actType, string pszSource, string pszDescription, Guid guid)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        int IVsActivityLog.LogEntryGuidHr(uint actType, string pszSource, string pszDescription, Guid guid, int hr)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        int IVsActivityLog.LogEntryGuidHrPath(uint actType, string pszSource, string pszDescription, Guid guid, int hr, string pszPath)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        int IVsActivityLog.LogEntryGuidPath(uint actType, string pszSource, string pszDescription, Guid guid, string pszPath)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        int IVsActivityLog.LogEntryHr(uint actType, string pszSource, string pszDescription, int hr)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        int IVsActivityLog.LogEntryHrPath(uint actType, string pszSource, string pszDescription, int hr, string pszPath)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        int IVsActivityLog.LogEntryPath(uint actType, string pszSource, string pszDescription, string pszPath)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }
        #endregion
    }
}
