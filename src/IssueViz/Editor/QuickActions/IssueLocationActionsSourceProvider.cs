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
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("SonarLint Issue Visualization")]
    [ContentType("text")]
    internal class IssueLocationActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        private readonly IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService;
        private readonly IAnalysisIssueSelectionService selectionService;

        [ImportingConstructor]
        public IssueLocationActionsSourceProvider(IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService, IAnalysisIssueSelectionService selectionService)
        {
            this.bufferTagAggregatorFactoryService = bufferTagAggregatorFactoryService;
            this.selectionService = selectionService;
        }

        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                return null;
            }

            return new IssueLocationActionsSource(bufferTagAggregatorFactoryService, textBuffer, selectionService);
        }
    }
}
