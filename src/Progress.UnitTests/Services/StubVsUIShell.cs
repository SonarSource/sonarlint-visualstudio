/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
    /// Test implementation for <see cref="IVsUIShell"/>
    /// </summary>
    public class StubVsUIShell : IVsUIShell
    {
        private bool messageBoxShown;

        #region Configuration

        public Func<Guid, IVsWindowFrame> FindToolWindowAction
        {
            get;
            set;
        }

        public Action<string, string> ShowMessageBoxAction
        {
            get;
            set;
        }

        #endregion Configuration

        #region IVsUIShell

        int IVsUIShell.AddNewBFNavigationItem(IVsWindowFrame pwindowFrame, string bstrData, object punk, int replaceCurrent)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.CenterDialogOnWindow(IntPtr hwndDialog, IntPtr hwndParent)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.CreateDocumentWindow(uint grfCDW, string pszMkDocument, IVsUIHierarchy uih, uint itemid, IntPtr punkDocView, IntPtr punkDocData, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidCmdUI, Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp, string pszOwnerCaption, string pszEditorCaption, int[] defaultPosition, out IVsWindowFrame windowFrame)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.CreateToolWindow(uint grfCTW, uint toolWindowId, object punkTool, ref Guid rclsidTool, ref Guid rguidPersistenceSlot, ref Guid rguidAutoActivate, Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp, string pszCaption, int[] defaultPosition, out IVsWindowFrame windowFrame)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.EnableModeless(int enable)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.FindToolWindow(uint grfFTW, ref Guid rguidPersistenceSlot, out IVsWindowFrame windowFrame)
        {
            windowFrame = null;
            if (this.FindToolWindowAction != null)
            {
                windowFrame = this.FindToolWindowAction(rguidPersistenceSlot);
            }

            return VSConstants.S_OK;
        }

        int IVsUIShell.FindToolWindowEx(uint grfFTW, ref Guid rguidPersistenceSlot, uint toolWinId, out IVsWindowFrame windowFrame)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetAppName(out string pbstrAppName)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetCurrentBFNavigationItem(out IVsWindowFrame windowFrame, out string pbstrData, out object ppunk)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetDialogOwnerHwnd(out IntPtr phwnd)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetDirectoryViaBrowseDlg(VSBROWSEINFOW[] browse)
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

        int IVsUIShell.GetNextBFNavigationItem(out IVsWindowFrame windowFrame, out string pbstrData, out object ppunk)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetOpenFileNameViaDlg(VSOPENFILENAMEW[] openFileName)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetPreviousBFNavigationItem(out IVsWindowFrame windowFrame, out string pbstrData, out object ppunk)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.GetSaveFileNameViaDlg(VSSAVEFILENAMEW[] saveFileName)
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

        int IVsUIShell.GetVSSysColor(VSSYSCOLOR sysColIndex, out uint pdwRGBval)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.OnModeChange(DBGMODE dbgmodeNew)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.PostExecCommand(ref Guid pguidCmdGroup, uint cmdID, uint cmdexecopt, ref object pvaIn)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.PostSetFocusMenuCommand(ref Guid pguidCmdGroup, uint cmdID)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.RefreshPropertyBrowser(int dispid)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.RemoveAdjacentBFNavigationItem(RemoveBFDirection dir)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.RemoveCurrentNavigationDupes(RemoveBFDirection dir)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.ReportErrorInfo(int hr)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.SaveDocDataToFile(VSSAVEFLAGS grfSave, object persistFile, string pszUntitledPath, out string pbstrDocumentNew, out int canceled)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.SetErrorInfo(int hr, string pszDescription, uint reserved, string pszHelpKeyword, string pszSource)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.SetForegroundWindow()
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.SetMRUComboText(ref Guid pguidCmdGroup, uint cmdID, string lpszText, int addToList)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.SetMRUComboTextW(Guid[] pguidCmdGroup, uint cmdID, string pwszText, int addToList)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.SetToolbarVisibleInFullScreen(Guid[] pguidCmdGroup, uint toolbarId, int visibleInFullScreen)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.SetWaitCursor()
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.SetupToolbar(IntPtr hwnd, IVsToolWindowToolbar ptwt, out IVsToolWindowToolbarHost pptwth)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.ShowContextMenu(uint dcompRole, ref Guid rclsidActive, int menuId, POINTS[] pos, Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget cmdTrgtActive)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.ShowMessageBox(uint compRole, ref Guid rclsidComp, string pszTitle, string pszText, string pszHelpFile, uint dhelpContextID, OLEMSGBUTTON msgbtn, OLEMSGDEFBUTTON msgdefbtn, OLEMSGICON msgicon, int sysAlert, out int result)
        {
            result = 0;
            this.messageBoxShown = true;
            if (this.ShowMessageBoxAction != null)
            {
                this.ShowMessageBoxAction(pszTitle, pszText);
            }

            return VSConstants.S_OK;
        }

        int IVsUIShell.TranslateAcceleratorAsACmd(Microsoft.VisualStudio.OLE.Interop.MSG[] msg)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.UpdateCommandUI(int immediateUpdate)
        {
            throw new NotImplementedException();
        }

        int IVsUIShell.UpdateDocDataIsDirtyFeedback(uint docCookie, int dirty)
        {
            throw new NotImplementedException();
        }

        #endregion IVsUIShell

        #region Verification

        public void AssertMessageBoxShown()
        {
            this.messageBoxShown.Should().BeTrue("No requests to show the message box");
        }

        public void AssertMessageBoxNotShown()
        {
            this.messageBoxShown.Should().BeFalse("Not expected any requests to show the message box");
        }

        #endregion Verification

        #region Test helpers

        public void Reset()
        {
            this.messageBoxShown = false;
        }

        internal class StubWindowFrame : IVsWindowFrame
        {
            internal bool WasShown { get; private set; }

            #region IVsWindowFrame

            int IVsWindowFrame.CloseFrame(uint grfSaveOptions)
            {
                throw new NotImplementedException();
            }

            int IVsWindowFrame.GetFramePos(VSSETFRAMEPOS[] pdwSFP, out Guid pguidRelativeTo, out int px, out int py, out int pcx, out int pcy)
            {
                throw new NotImplementedException();
            }

            int IVsWindowFrame.GetGuidProperty(int propid, out Guid pguid)
            {
                throw new NotImplementedException();
            }

            int IVsWindowFrame.GetProperty(int propid, out object pvar)
            {
                throw new NotImplementedException();
            }

            int IVsWindowFrame.Hide()
            {
                throw new NotImplementedException();
            }

            int IVsWindowFrame.IsOnScreen(out int onScreen)
            {
                throw new NotImplementedException();
            }

            int IVsWindowFrame.IsVisible()
            {
                throw new NotImplementedException();
            }

            int IVsWindowFrame.QueryViewInterface(ref Guid riid, out IntPtr ppv)
            {
                throw new NotImplementedException();
            }

            int IVsWindowFrame.SetFramePos(VSSETFRAMEPOS sfp, ref Guid rguidRelativeTo, int x, int y, int cx, int cy)
            {
                throw new NotImplementedException();
            }

            int IVsWindowFrame.SetGuidProperty(int propid, ref Guid rguid)
            {
                throw new NotImplementedException();
            }

            int IVsWindowFrame.SetProperty(int propid, object var)
            {
                throw new NotImplementedException();
            }

            int IVsWindowFrame.Show()
            {
                this.WasShown = true;
                return 0;
            }

            int IVsWindowFrame.ShowNoActivate()
            {
                throw new NotImplementedException();
            }

            #endregion IVsWindowFrame

            #region Test helpers

            public void Reset()
            {
                this.WasShown = false;
            }

            #endregion Test helpers
        }

        #endregion Test helpers
    }
}