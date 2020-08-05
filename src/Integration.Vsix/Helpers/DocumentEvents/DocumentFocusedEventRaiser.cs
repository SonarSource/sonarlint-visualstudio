/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

namespace SonarLint.VisualStudio.Integration.Vsix.Helpers.DocumentEvents
{
    [Export(typeof(IDocumentFocusedEventRaiser))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class DocumentFocusedEventRaiser : IDocumentFocusedEventRaiser, IVsSelectionEvents
    {
        private readonly ITextDocumentProvider textDocumentProvider;
        private IVsMonitorSelection monitorSelection;
        private uint cookie;
        private bool disposed;

        public event EventHandler<DocumentFocusedEventArgs> OnDocumentFocused;

        [ImportingConstructor]
        public DocumentFocusedEventRaiser([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, ITextDocumentProvider textDocumentProvider)
        {
            this.textDocumentProvider = textDocumentProvider;

            RunOnUIThread.Run(() =>
            {
                monitorSelection = serviceProvider.GetService<SVsShellMonitorSelection, IVsMonitorSelection>();

                if (monitorSelection == null)
                {
                    throw new ArgumentNullException(nameof(monitorSelection));
                }

                monitorSelection.AdviseSelectionEvents(this, out cookie);
            });
        }

        public int OnElementValueChanged(uint elementId, object oldValue, object newValue)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (newValue is IVsWindowFrame frame &&
                IsFrameElement() && 
                IsDocumentFrame())
            {
                var textDocument = textDocumentProvider.GetFromFrame(frame);

                OnDocumentFocused?.Invoke(this, new DocumentFocusedEventArgs(textDocument.FilePath, textDocument.TextBuffer.ContentType));
            }

            return VSConstants.S_OK;

            bool IsFrameElement()
            {
                return elementId == (uint)VSConstants.VSSELELEMID.SEID_WindowFrame;
            }

            bool IsDocumentFrame()
            {
                return ErrorHandler.Succeeded(frame.GetProperty((int)__VSFPROPID.VSFPROPID_Type, out var frameType)) &&
                       (int)frameType == (int)__WindowFrameTypeFlags.WINDOWFRAMETYPE_Document;
            }
        }

        public int OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            return VSConstants.S_OK;
        }

        public int OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
        {
            return VSConstants.S_OK;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                monitorSelection.UnadviseSelectionEvents(cookie);
                OnDocumentFocused = null;
                disposed = true;
            }
        }
    }
}
