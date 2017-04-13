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
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation for <see cref="IVsOutputWindowPane"/>
    /// </summary>
    public class StubVsOutputWindowPane : IVsOutputWindowPane
    {
        internal bool IsActivated { get; private set; }
        internal bool IsWrittenToOutputWindow { get; private set; }

        #region Configuration

        public Action<string> OutputStringThreadSafeAction
        {
            get;
            set;
        }

        #endregion Configuration

        #region IVsOutputWindowPane

        int IVsOutputWindowPane.Activate()
        {
            this.IsActivated = true;
            return VSConstants.S_OK;
        }

        int IVsOutputWindowPane.Clear()
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.FlushToTaskList()
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.GetName(ref string pbstrPaneName)
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.Hide()
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.OutputString(string pszOutputString)
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.OutputStringThreadSafe(string pszOutputString)
        {
            this.IsWrittenToOutputWindow = true;
            this.OutputStringThreadSafeAction?.Invoke(pszOutputString);

            return VSConstants.S_OK;
        }

        int IVsOutputWindowPane.OutputTaskItemString(string pszOutputString, VSTASKPRIORITY priority, VSTASKCATEGORY category, string pszSubcategory, int bitmap, string pszFilename, uint lineNum, string pszTaskItemText)
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.OutputTaskItemStringEx(string pszOutputString, VSTASKPRIORITY priority, VSTASKCATEGORY category, string pszSubcategory, int bitmap, string pszFilename, uint lineNum, string pszTaskItemText, string pszLookupKwd)
        {
            throw new NotImplementedException();
        }

        int IVsOutputWindowPane.SetName(string pszPaneName)
        {
            throw new NotImplementedException();
        }

        #endregion IVsOutputWindowPane

        #region Test helpers

        public void Reset()
        {
            this.IsActivated = false;
            this.IsWrittenToOutputWindow = false;
        }

        #endregion Test helpers
    }
}