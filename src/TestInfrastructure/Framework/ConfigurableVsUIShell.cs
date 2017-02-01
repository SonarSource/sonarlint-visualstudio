/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting; using FluentAssertions;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsUIShell : IVsUIShell
    {
        private readonly Dictionary<Guid, IVsWindowFrame> toolwindowFrames = new Dictionary<Guid, IVsWindowFrame>();

        #region IVsUIShell
        int IVsUIShell.AddNewBFNavigationItem(IVsWindowFrame pWindowFrame, string bstrData, object punk, int fReplaceCurrent)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.CenterDialogOnWindow(IntPtr hwndDialog, IntPtr hwndParent)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.CreateDocumentWindow(uint grfCDW, string pszMkDocument, IVsUIHierarchy pUIH, uint itemid, IntPtr punkDocView, IntPtr punkDocData, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidCmdUI, Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp, string pszOwnerCaption, string pszEditorCaption, int[] pfDefaultPosition, out IVsWindowFrame ppWindowFrame)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.CreateToolWindow(uint grfCTW, uint dwToolWindowId, object punkTool, ref Guid rclsidTool, ref Guid rguidPersistenceSlot, ref Guid rguidAutoActivate, Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp, string pszCaption, int[] pfDefaultPosition, out IVsWindowFrame ppWindowFrame)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.EnableModeless(int fEnable)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.FindToolWindow(uint grfFTW, ref Guid rguidPersistenceSlot, out IVsWindowFrame ppWindowFrame)
        {
            ((__VSFINDTOOLWIN)grfFTW).Should().Be(this.ExpectedFindToolWindowArgument);

            if (this.toolwindowFrames.TryGetValue(rguidPersistenceSlot, out ppWindowFrame))
            {
                return VSConstants.S_OK;
            }

            return VSConstants.E_FAIL;
        }

        int IVsUIShell.FindToolWindowEx(uint grfFTW, ref Guid rguidPersistenceSlot, uint dwToolWinId, out IVsWindowFrame ppWindowFrame)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetAppName(out string pbstrAppName)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetCurrentBFNavigationItem(out IVsWindowFrame ppWindowFrame, out string pbstrData, out object ppunk)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetDialogOwnerHwnd(out IntPtr phwnd)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetDirectoryViaBrowseDlg(VSBROWSEINFOW[] pBrowse)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetDocumentWindowEnum(out IEnumWindowFrames ppenum)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetErrorInfo(out string pbstrErrText)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetNextBFNavigationItem(out IVsWindowFrame ppWindowFrame, out string pbstrData, out object ppunk)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetOpenFileNameViaDlg(VSOPENFILENAMEW[] pOpenFileName)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetPreviousBFNavigationItem(out IVsWindowFrame ppWindowFrame, out string pbstrData, out object ppunk)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetSaveFileNameViaDlg(VSSAVEFILENAMEW[] pSaveFileName)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetToolWindowEnum(out IEnumWindowFrames ppenum)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetURLViaDlg(string pszDlgTitle, string pszStaticLabel, string pszHelpTopic, out string pbstrURL)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetVSSysColor(VSSYSCOLOR dwSysColIndex, out uint pdwRGBval)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.OnModeChange(DBGMODE dbgmodeNew)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.PostExecCommand(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, ref object pvaIn)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.PostSetFocusMenuCommand(ref Guid pguidCmdGroup, uint nCmdID)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.RefreshPropertyBrowser(int dispid)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.RemoveAdjacentBFNavigationItem(RemoveBFDirection rdDir)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.RemoveCurrentNavigationDupes(RemoveBFDirection rdDir)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.ReportErrorInfo(int hr)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.SaveDocDataToFile(VSSAVEFLAGS grfSave, object pPersistFile, string pszUntitledPath, out string pbstrDocumentNew, out int pfCanceled)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.SetErrorInfo(int hr, string pszDescription, uint dwReserved, string pszHelpKeyword, string pszSource)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.SetForegroundWindow()
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.SetMRUComboText(ref Guid pguidCmdGroup, uint dwCmdID, string lpszText, int fAddToList)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.SetMRUComboTextW(Guid[] pguidCmdGroup, uint dwCmdID, string pwszText, int fAddToList)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.SetToolbarVisibleInFullScreen(Guid[] pguidCmdGroup, uint dwToolbarId, int fVisibleInFullScreen)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.SetupToolbar(IntPtr hwnd, IVsToolWindowToolbar ptwt, out IVsToolWindowToolbarHost pptwth)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.SetWaitCursor()
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.ShowContextMenu(uint dwCompRole, ref Guid rclsidActive, int nMenuId, POINTS[] pos, IOleCommandTarget pCmdTrgtActive)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.ShowMessageBox(uint dwCompRole, ref Guid rclsidComp, string pszTitle, string pszText, string pszHelpFile, uint dwHelpContextID, OLEMSGBUTTON msgbtn, OLEMSGDEFBUTTON msgdefbtn, OLEMSGICON msgicon, int fSysAlert, out int pnResult)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.TranslateAcceleratorAsACmd(MSG[] pMsg)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.UpdateCommandUI(int fImmediateUpdate)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.UpdateDocDataIsDirtyFeedback(uint docCookie, int fDirty)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Test helpers
        public ConfigurableVsWindowFrame RegisterToolWindow(Guid toolWindowGuid)
        {
            ConfigurableVsWindowFrame frame = new ConfigurableVsWindowFrame();
            this.toolwindowFrames[toolWindowGuid] = frame;
            return frame;
        }

        public __VSFINDTOOLWIN ExpectedFindToolWindowArgument
        {
            get;
            set;
        } = __VSFINDTOOLWIN.FTW_fForceCreate;
        #endregion
    }
}
