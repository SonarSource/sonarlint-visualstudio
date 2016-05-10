//-----------------------------------------------------------------------
// <copyright file="ActiveSolutionTracker.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IActiveSolutionTracker))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ActiveSolutionTracker : IActiveSolutionTracker, IVsSolutionEvents, IDisposable
    {
        private bool isDisposed = false;
        private readonly IVsSolution solution;
        private uint cookie;

        /// <summary>
        /// <see cref="IActiveSolutionTracker.ActiveSolutionChanged"/>
        /// </summary>
        public event EventHandler ActiveSolutionChanged;

        [ImportingConstructor]
        public ActiveSolutionTracker([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            this.solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            Debug.Assert(this.solution != null, "Cannot find IVsSolution");
            ErrorHandler.ThrowOnFailure(this.solution.AdviseSolutionEvents(this, out this.cookie));
        }

        protected virtual void OnActiveSolutionChanged()
        {
            this.ActiveSolutionChanged?.Invoke(this, EventArgs.Empty);
        }

        #region IVsSolutionEvents
        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
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
            this.OnActiveSolutionChanged();
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
            this.OnActiveSolutionChanged();

            return VSConstants.S_OK;
        }
        #endregion

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
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
