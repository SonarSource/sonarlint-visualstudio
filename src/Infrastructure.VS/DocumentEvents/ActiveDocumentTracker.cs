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

        int IVsSelectionEvents.OnElementValueChanged(uint elementId, object oldValue, object newValue)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Note: this notification can fire multiple times with different elementId value
            // when the doc frame changes. Two cases:
            // [ tool or doc window ] -> [ tool or doc window ] => elementId == SEID_WindowFrame
            // [ doc window ] -> [ doc window ] => elementId == SEID_DocumentFrame
            // We are only interested in the second case. See bugs #2079 and #2091.
            if (elementId == (uint)VSConstants.VSSELELEMID.SEID_DocumentFrame)
            {
                ITextDocument activeTextDoc = null;
                if (newValue != null && newValue is IVsWindowFrame frame)
                {
                    activeTextDoc = textDocumentProvider.GetFromFrame(frame);
                }

                // The "active document" will be null if the last document has just been closed
                ActiveDocumentChanged?.Invoke(this, new ActiveDocumentChangedEventArgs(activeTextDoc));
            }

            return VSConstants.S_OK;
        }

        int IVsSelectionEvents.OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            return VSConstants.S_OK;
        }

        int IVsSelectionEvents.OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
        {
            return VSConstants.S_OK;
        }

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
