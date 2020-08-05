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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal interface ITextDocumentProvider
    {
        ITextDocument GetFromFrame(IVsWindowFrame frame);
    }

    [Export(typeof(ITextDocumentProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class TextDocumentProvider : ITextDocumentProvider
    {
        private readonly IVsEditorAdaptersFactoryService editorAdapterService;

        [ImportingConstructor]
        public TextDocumentProvider(IVsEditorAdaptersFactoryService editorAdapterService)
        {
            this.editorAdapterService = editorAdapterService;
        }

        public ITextDocument GetFromFrame(IVsWindowFrame frame)
        {
            // Get the doc data, which should also be a text buffer
            frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out var docData);
            if (!(docData is IVsTextBuffer vsTextBuffer))
            {
                return null;
            }

            // Finally, convert from the legacy VS editor interface to the new-style interface
            var textBuffer = editorAdapterService.GetDocumentBuffer(vsTextBuffer);

            ITextDocument newTextDocument = null;
            textBuffer?.Properties?.TryGetProperty(
                typeof(ITextDocument), out newTextDocument);

            return newTextDocument;
        }
    }
}
