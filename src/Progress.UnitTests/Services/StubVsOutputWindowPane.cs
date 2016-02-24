//-----------------------------------------------------------------------
// <copyright file="StubVsOutputWindowPane.cs" company="SonarSource SA and Microsoft Corporation">
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
    /// Test implementation for <see cref="IVsOutputWindowPane"/>
    /// </summary>
    public class StubVsOutputWindowPane : IVsOutputWindowPane
    {
        private bool activated;
        private bool writtenToOutputWindow;

        #region Configuration
        public Action<string> OutputStringThreadSafeAction
        {
            get;
            set;
        }
        #endregion

        #region IVsOutputWindowPane
        int IVsOutputWindowPane.Activate()
        {
            this.activated = true;
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
            this.writtenToOutputWindow = true;
            if (this.OutputStringThreadSafeAction != null)
            {
                this.OutputStringThreadSafeAction(pszOutputString);
            }

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
        #endregion

        #region Verification
        public void AssertActivated()
        {
            Assert.IsTrue(this.activated, "Expected the output window to be activated");
        }

        public void AssertNotActivated()
        {
            Assert.IsFalse(this.activated, "Not expected the output window to be activated");
        }

        public void AssertWrittenToOutputWindow()
        {
            Assert.IsTrue(this.writtenToOutputWindow, "Expected to write to output window");
        }

        public void AssertNotWrittenToOutputWindow()
        {
            Assert.IsFalse(this.writtenToOutputWindow, "Not expected to write to output window");
        }
        #endregion

        #region Test helpers
        public void Reset()
        {
            this.activated = false;
            this.writtenToOutputWindow = false;
        }
        #endregion
    }
}
