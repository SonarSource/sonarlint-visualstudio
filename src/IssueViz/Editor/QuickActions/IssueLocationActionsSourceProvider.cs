﻿/*
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
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("SonarQube for Visual Studio Issue Visualization")]
    [ContentType("text")]
    internal class IssueLocationActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        private readonly IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService;
        private readonly IIssueSelectionService selectionService;
        private readonly ILightBulbBroker lightBulbBroker;
        private readonly IVsUIServiceOperation vsUIServiceOperation;

        [ImportingConstructor]
        public IssueLocationActionsSourceProvider(IVsUIServiceOperation vsUIServiceOperation,
            IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService,
            IIssueSelectionService selectionService,
            ILightBulbBroker lightBulbBroker)
        {
            this.bufferTagAggregatorFactoryService = bufferTagAggregatorFactoryService;
            this.selectionService = selectionService;
            this.lightBulbBroker = lightBulbBroker;
            this.vsUIServiceOperation = vsUIServiceOperation;
        }

        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            var result = vsUIServiceOperation.Execute<SVsUIShell, IVsUIShell, ISuggestedActionsSource>(shell => DoCreateSuggestedActionsSource(shell, textView, textBuffer));

            return result;
        }

        private ISuggestedActionsSource DoCreateSuggestedActionsSource(IVsUIShell shell, ITextView textView, ITextBuffer textBuffer)
        {
            if (textView == null || textBuffer == null)
            {
                return null;
            }

            return new IssueLocationActionsSource(lightBulbBroker, shell, bufferTagAggregatorFactoryService, textView, selectionService);
        }
    }
}
