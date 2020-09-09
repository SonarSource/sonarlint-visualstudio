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
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Buffer
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(ISelectedIssueLocationTag))]
    internal class SelectedIssueLocationTaggerProvider : ITaggerProvider
    {
        private readonly IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService;
        private readonly IAnalysisIssueSelectionService issueSelectionService;
        private readonly ITaggableBufferIndicator taggableBufferIndicator;

        [ImportingConstructor]
        public SelectedIssueLocationTaggerProvider(IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService, 
            IAnalysisIssueSelectionService issueSelectionService, 
            ITaggableBufferIndicator taggableBufferIndicator)
        {
            this.bufferTagAggregatorFactoryService = bufferTagAggregatorFactoryService;
            this.issueSelectionService = issueSelectionService;
            this.taggableBufferIndicator = taggableBufferIndicator;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (!taggableBufferIndicator.IsTaggable(buffer))
            {
                return null;
            }

            // Tip when debugging/developing: a buffer tagger won't be created until there is something that can consume the tags. In our case,
            // it means until one of our view tagger providers is created, because they create a tag aggregator that consumes the those
            // buffer tags.
            var tagger = buffer.Properties.GetOrCreateSingletonProperty(typeof(SelectedIssueLocationTagger), () => Create(buffer));
            return tagger as ITagger<T>;
        }

        private SelectedIssueLocationTagger Create(ITextBuffer buffer)
        {
            var aggregator = bufferTagAggregatorFactoryService.CreateTagAggregator<IIssueLocationTag>(buffer);
            return new SelectedIssueLocationTagger(aggregator, buffer, issueSelectionService);
        }
    }
}
