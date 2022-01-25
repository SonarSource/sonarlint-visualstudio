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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("SonarLint Quick Fix")]
    [ContentType("text")]
    internal class IssueFixActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        private readonly IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService;
        private readonly ILightBulbBroker lightBulbBroker;
        private readonly IIssueSpanCalculator spanCalculator;

        [ImportingConstructor]
        public IssueFixActionsSourceProvider(IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService,
            ILightBulbBroker lightBulbBroker,
            IIssueSpanCalculator spanCalculator)
        {
            this.bufferTagAggregatorFactoryService = bufferTagAggregatorFactoryService;
            this.lightBulbBroker = lightBulbBroker;
            this.spanCalculator = spanCalculator;
        }

        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            if (textView == null || textBuffer == null)
            {
                return null;
            }

            return new IssueFixActionsSource(lightBulbBroker, bufferTagAggregatorFactoryService, spanCalculator, textView);
        }
    }
}
