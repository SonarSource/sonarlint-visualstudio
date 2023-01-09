﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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
        private readonly IIssueSelectionService selectionService;
        private readonly ILightBulbBroker lightBulbBroker;
        private readonly IVsUIShell vsUiShell;

        [ImportingConstructor]
        public IssueLocationActionsSourceProvider([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService,
            IIssueSelectionService selectionService,
            ILightBulbBroker lightBulbBroker)
        {
            this.bufferTagAggregatorFactoryService = bufferTagAggregatorFactoryService;
            this.selectionService = selectionService;
            this.lightBulbBroker = lightBulbBroker;
            vsUiShell = serviceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;
        }

        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            if (textView == null || textBuffer == null)
            {
                return null;
            }

            return new IssueLocationActionsSource(lightBulbBroker, vsUiShell, bufferTagAggregatorFactoryService, textView, selectionService);
        }
    }
}
