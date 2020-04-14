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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal interface IActiveDocumentLocator
    {
        /// <summary>
        /// Returns the current active document, or null if there is no active document
        /// </summary>
        /// <returns></returns>
        ITextDocument FindActiveDocument();
    }

    internal class ActiveDocumentLocator : IActiveDocumentLocator
    {
        private readonly IVsMonitorSelection monitorSelection;
        private readonly IVsEditorAdaptersFactoryService editorAdapterService;

        public ActiveDocumentLocator(IVsMonitorSelection monitorSelection, IVsEditorAdaptersFactoryService editorAdapterService)
        {
            this.monitorSelection = monitorSelection;
            this.editorAdapterService = editorAdapterService;
        }

        public ITextDocument FindActiveDocument()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get the current doc frame, if there is one
            monitorSelection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out var pvarValue);
            if (!(pvarValue is IVsWindowFrame frame))
            {
                return null;
            }

            // Now get the doc data, which should also be a text buffer
            frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out var docData);
            if (!(docData is IVsTextBuffer vsTextBuffer))
            {
                return null;
            }

            // Finally, convert from the legacy VS editor interface to the new-style interface
            var textBuffer = editorAdapterService.GetDocumentBuffer(vsTextBuffer);
            textBuffer.Properties.TryGetProperty(
                typeof(ITextDocument), out ITextDocument newTextDocument);

            return newTextDocument;
        }
    }
}
