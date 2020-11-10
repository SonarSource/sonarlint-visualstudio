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
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.TextManager.Interop;
using SonarLint.VisualStudio.IssueVisualization.Helpers;

namespace SonarLint.VisualStudio.IssueVisualization.Editor
{
    internal interface IDocumentNavigator
    {
        ITextView Open(string filePath);
        void Navigate(ITextView textView, SnapshotSpan snapshotSpan);
    }

    [Export(typeof(IDocumentNavigator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class DocumentNavigator : IDocumentNavigator
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IVsEditorAdaptersFactoryService editorAdaptersFactory;
        private readonly IOutliningManagerService outliningManagerService;
        private readonly IEditorOperationsFactoryService editorOperationsFactory;

        [ImportingConstructor]
        public DocumentNavigator([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IVsEditorAdaptersFactoryService editorAdaptersFactory, 
            IOutliningManagerService outliningManagerService, 
            IEditorOperationsFactoryService editorOperationsFactory)
        {
            this.serviceProvider = serviceProvider;
            this.editorAdaptersFactory = editorAdaptersFactory;
            this.outliningManagerService = outliningManagerService;
            this.editorOperationsFactory = editorOperationsFactory;
        }

        public ITextView Open(string filePath)
        {
            var textView = OpenDocument(filePath);
            var wpfTextView = editorAdaptersFactory.GetWpfTextView(textView);

            return wpfTextView;
        }

        public void Navigate(ITextView textView, SnapshotSpan snapshotSpan)
        {
            RunOnUIThread.Run(() =>
            {
                ExpandSpan(textView, snapshotSpan);

                SelectSpan(textView, snapshotSpan);
            });
        }

        private IVsTextView OpenDocument(string filePath)
        {
            VsShellUtilities.OpenDocument(serviceProvider, filePath, Guid.Empty, out _, out _, out _, out var vsTextView);

            return vsTextView;
        }

        private void ExpandSpan(ITextView textView, SnapshotSpan snapshotSpan)
        {
            var outliningManager = outliningManagerService.GetOutliningManager(textView);
            outliningManager?.ExpandAll(snapshotSpan, _ => true);
        }

        private void SelectSpan(ITextView wpfTextView, SnapshotSpan snapshotSpan)
        {
            var virtualSnapshotSpan = new VirtualSnapshotSpan(snapshotSpan);
            var editorOperations = editorOperationsFactory.GetEditorOperations(wpfTextView);

            editorOperations.SelectAndMoveCaret(
                virtualSnapshotSpan.Start,
                virtualSnapshotSpan.End,
                TextSelectionMode.Stream,
                EnsureSpanVisibleOptions.AlwaysCenter);
        }
    }
}
