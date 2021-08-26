/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IActiveSolutionTracker))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ActiveSolutionTracker : IActiveSolutionTracker, IVsSolutionEvents, IDisposable, IVsSolutionEvents7
    {
        private bool isDisposed;
        private readonly IVsSolution solution;
        private readonly uint cookie;

        /// <summary>
        /// <see cref="IActiveSolutionTracker.ActiveSolutionChanged"/>
        /// </summary>
        public event EventHandler<ActiveSolutionChangedEventArgs> ActiveSolutionChanged;

        /// <summary>
        /// <see cref="IActiveSolutionTracker.BeforeSolutionClosed"/>
        /// </summary>
        public event EventHandler BeforeSolutionClosed;

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
            BeforeSolutionClosed?.Invoke(this, EventArgs.Empty);
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

// Decompiled with JetBrains decompiler
// Type: Microsoft.VisualStudio.Shell.Interop.IVsSolutionEvents7
// MVID: 1C59188A-5A3D-4544-B9B8-A6F9357934A8

/// <summary>
/// IVsSolutionEvents7 provides information for Open-As-Folder events.
/// However, it's not available in VS 2015, so we can't reference the assembly.
/// Since we only need to embed the COM type, it doesn't matter which assembly it's coming
/// from and we can place the declaration in our assembly.
/// </summary>
namespace Microsoft.VisualStudio.Shell.Interop
{
    [CompilerGenerated]
    [Guid("A459C228-5617-4136-BCBE-C282DF6D9A62")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [TypeIdentifier]
    [ComImport]
    public interface IVsSolutionEvents7
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void OnAfterOpenFolder([MarshalAs(UnmanagedType.LPWStr), In] string folderPath);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void OnBeforeCloseFolder([MarshalAs(UnmanagedType.LPWStr), In] string folderPath);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void OnQueryCloseFolder([MarshalAs(UnmanagedType.LPWStr), In] string folderPath, [In, Out] ref int pfCancel);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void OnAfterCloseFolder([MarshalAs(UnmanagedType.LPWStr), In] string folderPath);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void OnAfterLoadAllDeferredProjects();
    }
}
