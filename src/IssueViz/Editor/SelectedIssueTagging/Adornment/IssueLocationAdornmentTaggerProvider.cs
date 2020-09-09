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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Adornment
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    [TagType(typeof(IntraTextAdornmentTag))]
    internal class IssueLocationAdornmentTaggerProvider : IViewTaggerProvider
    {
        private readonly IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService;
        private readonly ITaggableBufferIndicator taggableBufferIndicator;

        [ImportingConstructor]
        public IssueLocationAdornmentTaggerProvider(IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService,
            ITaggableBufferIndicator taggableBufferIndicator)
        {
            this.bufferTagAggregatorFactoryService = bufferTagAggregatorFactoryService;
            this.taggableBufferIndicator = taggableBufferIndicator;
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (buffer != textView.TextBuffer)
            {
                return null;
            }

            if (!taggableBufferIndicator.IsTaggable(buffer))
            {
                return null;
            }

            var tagger = textView.Properties.GetOrCreateSingletonProperty(typeof(IssueLocationAdornmentTagger), () => Create(textView as IWpfTextView));
            return tagger as ITagger<T>;
        }

        private IssueLocationAdornmentTagger Create(IWpfTextView textView)
        {
            var aggregator = bufferTagAggregatorFactoryService.CreateTagAggregator<ISelectedIssueLocationTag>(textView.TextBuffer);
            return new IssueLocationAdornmentTagger(aggregator, textView);
        }
    }
}
