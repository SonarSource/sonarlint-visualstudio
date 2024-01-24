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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;

namespace SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents
{
    public interface IActiveDocumentTracker : IDisposable
    {
        /// <summary>
        /// Raises an event when the active text document changes
        /// </summary>
        /// <remarks>
        /// Returned <see cref="ActiveDocumentChangedEventArgs.TextDocument"/> can be null if there is not an active text document
        /// e.g. if the last document has been closed, or if the active document is not a text document
        /// </remarks>
        event EventHandler<ActiveDocumentChangedEventArgs> ActiveDocumentChanged;
    }

    [Export(typeof(IActiveDocumentTracker))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ActiveDocumentTracker : IActiveDocumentTracker, IVsSelectionEvents
    {
        private readonly ITextDocumentProvider textDocumentProvider;
        private IVsMonitorSelection monitorSelection;
        private uint cookie;
        private bool disposed;

        public event EventHandler<ActiveDocumentChangedEventArgs> ActiveDocumentChanged;

        [ImportingConstructor]
        public ActiveDocumentTracker([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, ITextDocumentProvider textDocumentProvider)
        {
            this.textDocumentProvider = textDocumentProvider;

            RunOnUIThread.Run(() =>
            {
                monitorSelection = serviceProvider.GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
                monitorSelection.AdviseSelectionEvents(this, out cookie);
            });
        }

        /// <summary>
        /// This notification can fire multiple times with a different elementId value:
        ///     [elementId == SEID_WindowFrame] is fired when transitioning from [tool or doc window] -> [tool or doc window]
        ///     [elementId == SEID_DocumentFrame] is fired when transitioning from [doc window] -> [doc window]
        ///
        /// We are interested in the following scenarios:
        ///       1. Transition from doc A to doc B (see #1559) --> can use SEID_DocumentFrame or SEID_WindowFrame
        ///       2. Closing last document (see #2091) --> can use SEID_DocumentFrame or SEID_WindowFrame
        ///       3. Single-click navigation (see #2079) --> can ONLY use SEID_DocumentFrame
        ///       4. Transition from tool window to document (see #2242) --> can ONLY use SEID_WindowFrame
        ///
        /// To support all the cases, we use SEID_DocumentFrame for uses cases 1-3 and SEID_WindowFrame for use case 4.
        /// </summary>
        int IVsSelectionEvents.OnElementValueChanged(uint elementId, object oldValue, object newValue)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (elementId == (uint)VSConstants.VSSELELEMID.SEID_DocumentFrame)
            {
                ITextDocument activeTextDoc = null;

                if (newValue is IVsWindowFrame newWindowFrame)
                {
                    activeTextDoc = textDocumentProvider.GetFromFrame(newWindowFrame);
                }

                // The "active document" will be null if the last document has just been closed
                NotifyActiveDocumentChanged(activeTextDoc);
            }
            // if we reached here, we know that oldValue and/or newValue are a tool window,
            // and we are only interested in the use case of [tool window] -> [doc]
            else if (elementId == (uint) VSConstants.VSSELELEMID.SEID_WindowFrame && 
                     newValue is IVsWindowFrame newWindowFrame && IsDocumentFrame(newWindowFrame))
            {
                var activeTextDoc = textDocumentProvider.GetFromFrame(newWindowFrame);
                NotifyActiveDocumentChanged(activeTextDoc);
            }

            return VSConstants.S_OK;
        }

        private void NotifyActiveDocumentChanged(ITextDocument activeTextDoc) => 
            ActiveDocumentChanged?.Invoke(this, new ActiveDocumentChangedEventArgs(activeTextDoc));

        private static bool IsDocumentFrame(IVsWindowFrame frame) =>
            ErrorHandler.Succeeded(frame.GetProperty((int) __VSFPROPID.VSFPROPID_Type, out var frameType)) &&
            (int) frameType == (int) __WindowFrameTypeFlags.WINDOWFRAMETYPE_Document;

        int IVsSelectionEvents.OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew) => VSConstants.S_OK;

        int IVsSelectionEvents.OnCmdUIContextChanged(uint dwCmdUICookie, int fActive) => VSConstants.S_OK;

        public void Dispose()
        {
            if (!disposed)
            {
                monitorSelection.UnadviseSelectionEvents(cookie);
                ActiveDocumentChanged = null;
                disposed = true;
            }
        }
    }
}
