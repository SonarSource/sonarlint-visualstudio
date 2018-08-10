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
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IActiveSolutionTracker))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ActiveSolutionTracker : IActiveSolutionTracker, IVsSolutionEvents, IDisposable
    {
        private bool isDisposed;
        private readonly IVsSolution solution;
        private readonly uint cookie;

        /// <summary>
        /// <see cref="IActiveSolutionTracker.ActiveSolutionChanged"/>
        /// </summary>
        public event EventHandler<ActiveSolutionChangedEventArgs> ActiveSolutionChanged;
        public event EventHandler<ProjectOpenedEventArgs> AfterProjectOpened;

        [ImportingConstructor]
        public ActiveSolutionTracker([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            this.solution = serviceProvider.GetService<SVsSolution, IVsSolution>();
            Debug.Assert(this.solution != null, "Cannot find IVsSolution");
            ErrorHandler.ThrowOnFailure(this.solution.AdviseSolutionEvents(this, out this.cookie));
        }

        #region IVsSolutionEvents
        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            this.AfterProjectOpened?.Invoke(this, new ProjectOpenedEventArgs(pHierarchy));
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
            // Note: if lightweight solution load is enabled then the solution might not
            // be fully opened at this point
            this.ActiveSolutionChanged?.Invoke(this, new ActiveSolutionChangedEventArgs(isSolutionOpen: true));
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
            this.ActiveSolutionChanged?.Invoke(this, new ActiveSolutionChangedEventArgs(isSolutionOpen: false));
            return VSConstants.S_OK;
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
    }
}
