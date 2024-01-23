/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IActiveSolutionTracker))]
    [Export(typeof(IFolderWorkspaceMonitor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ActiveSolutionTracker : IActiveSolutionTracker, IFolderWorkspaceMonitor, IVsSolutionEvents, IDisposable, IVsSolutionEvents7
    {
        private readonly IFolderWorkspaceService folderWorkspaceService;
        private bool isDisposed;
        private readonly IVsSolution solution;
        private readonly uint cookie;

        /// <summary>
        /// <see cref="IActiveSolutionTracker.ActiveSolutionChanged"/>
        /// </summary>
        public event EventHandler<ActiveSolutionChangedEventArgs> ActiveSolutionChanged;

        public event EventHandler FolderWorkspaceInitialized;

        [ImportingConstructor]
        public ActiveSolutionTracker([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, IFolderWorkspaceService folderWorkspaceService)
        {
            this.folderWorkspaceService = folderWorkspaceService;
            this.solution = serviceProvider.GetService<SVsSolution, IVsSolution>();
            Debug.Assert(this.solution != null, "Cannot find IVsSolution");
            ErrorHandler.ThrowOnFailure(this.solution.AdviseSolutionEvents(this, out this.cookie));
        }

        #region IVsSolutionEvents
        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            if (folderWorkspaceService.IsFolderWorkspace())
            {
                FolderWorkspaceInitialized?.Invoke(this, EventArgs.Empty);
            }
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            RaiseSolutionChangedEvent(true);
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            RaiseSolutionChangedEvent(false);
            return VSConstants.S_OK;
        }
        #endregion

        #region IVsSolutionEvents7

        void IVsSolutionEvents7.OnAfterOpenFolder(string folderPath)
        {
            // For Open-As-Folder projects, IVsSolutionEvents.OnAfterOpenSolution is not being raised.
            // So we would get only this event when we're in Open-As-Folder mode, and OnAfterOpenSolution in the other cases.
            RaiseSolutionChangedEvent(true);
        }

        void IVsSolutionEvents7.OnBeforeCloseFolder(string folderPath)
        {
        }

        void IVsSolutionEvents7.OnQueryCloseFolder(string folderPath, ref int pfCancel)
        {
        }

        void IVsSolutionEvents7.OnAfterCloseFolder(string folderPath)
        {
            // IVsSolutionEvents.OnAfterCloseSolution would be raised when a folder is closed,
            // so we don't need to handle specifically folder close.
        }

        void IVsSolutionEvents7.OnAfterLoadAllDeferredProjects()
        {
        }

        #endregion

        #region IDisposable Support

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.solution.UnadviseSolutionEvents(this.cookie);
                }

                this.isDisposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
        }

        #endregion

        private void RaiseSolutionChangedEvent(bool isSolutionOpen)
        {
            // Note: if lightweight solution load is enabled then the solution might not
            // be fully opened at this point
            ActiveSolutionChanged?.Invoke(this, new ActiveSolutionChangedEventArgs(isSolutionOpen));
        }
    }
}
