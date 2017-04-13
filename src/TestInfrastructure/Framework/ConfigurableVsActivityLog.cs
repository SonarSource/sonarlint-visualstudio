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
using Microsoft.VisualStudio.Shell.Interop;

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

        #endregion IVsActivityLog
    }
}