/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Infrastructure.VS.Initialization;
using SonarLint.VisualStudio.Integration.Resources;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IActiveSolutionTracker))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ActiveSolutionTracker : IActiveSolutionTracker, IVsSolutionEvents2, IDisposable, IVsSolutionEvents7
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ISolutionInfoProvider solutionInfoProvider;
        private readonly ILogger logger;
        private bool isDisposed;
        private IVsSolution solution;
        private uint cookie;

        public string CurrentSolutionName { get; private set; }

        /// <summary>
        /// <see cref="IActiveSolutionTracker.ActiveSolutionChanged"/>
        /// </summary>
        public event EventHandler<ActiveSolutionChangedEventArgs> ActiveSolutionChanged;

        [ImportingConstructor]
        public ActiveSolutionTracker(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ISolutionInfoProvider solutionInfoProvider,
            IInitializationProcessorFactory initializationProcessorFactory,
            ILogger logger)
        {
            this.serviceProvider = serviceProvider;
            this.solutionInfoProvider = solutionInfoProvider;
            this.logger = logger.ForContext(Strings.ActiveSolutionTracker_LogContext);
            InitializationProcessor = initializationProcessorFactory.CreateAndStart<ActiveSolutionTracker>([], InitializeInternalAsync);
        }

        public IInitializationProcessor InitializationProcessor { get; }

        private Task InitializeInternalAsync(IThreadHandling threadHandling) =>
            threadHandling.RunOnUIThreadAsync(() =>
            {
                if (isDisposed)
                {
                    // not subscribing to events if already disposed
                    return;
                }
                CurrentSolutionName = solutionInfoProvider.GetSolutionName();
                logger.WriteLine(Strings.ActiveSolutionTracker_InitializedSolution, CurrentSolutionName ?? Strings.ActiveSolutionTracker_NoSolutionPlaceholder);
                solution = serviceProvider.GetService<SVsSolution, IVsSolution>();
                Debug.Assert(solution != null, "Cannot find IVsSolution");
                ErrorHandler.ThrowOnFailure(solution.AdviseSolutionEvents(this, out cookie));
            });

        #region IVsSolutionEvents2

        int IVsSolutionEvents2.OnAfterMergeSolution(object pUnkReserved)
        {
            // handling of dummy solutions
            if (CurrentSolutionName == null)
            {
                RaiseSolutionChangedEvent(true);
            }
            return VSConstants.S_OK;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            RaiseSolutionChangedEvent(true);
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
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
                if (disposing && InitializationProcessor.IsFinalized)
                {
                    this.solution?.UnadviseSolutionEvents(this.cookie);
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

        private void RaiseSolutionChangedEvent(bool isSolutionOpen, [CallerMemberName] string eventType = "")
        {
            var context = new MessageLevelContext { VerboseContext = [eventType] };
            // Note: if lightweight solution load is enabled then the solution might not
            // be fully opened at this point
            if (isSolutionOpen)
            {
                var solutionName = solutionInfoProvider.GetSolutionName();
                if (solutionName == null)
                {
                    logger.WriteLine(context, Strings.ActiveSolutionTracker_DummySolutionIgnored);
                    // dummy solutions don't:
                    // 1) have solution name -> we can't use them, might as well be no-solution state
                    // 2) are not closed when another solution is opened -> IVsSolutionEvents2.OnAfterMergeSolution handles that case
                    return;
                }
                CurrentSolutionName = solutionName;
                logger.WriteLine(context, Strings.ActiveSolutionTracker_SolutionOpen, solutionName);
            }
            else
            {
                logger.WriteLine(context, Strings.ActiveSolutionTracker_SolutionClosed, CurrentSolutionName ?? Strings.ActiveSolutionTracker_DummySolutionPlaceholder);
                CurrentSolutionName = null;
            }

            ActiveSolutionChanged?.Invoke(this, new ActiveSolutionChangedEventArgs(isSolutionOpen, CurrentSolutionName));
        }
    }
}
