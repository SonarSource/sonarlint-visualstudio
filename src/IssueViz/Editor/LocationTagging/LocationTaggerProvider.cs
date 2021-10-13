﻿/*
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

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("text")]
    [TagType(typeof(IIssueLocationTag))]
    internal class LocationTaggerProvider : ITaggerProvider
    {
        private readonly IIssueLocationStoreAggregator locationStore;
        private readonly IIssueSpanCalculator spanCalculator;
        private readonly ITaggableBufferIndicator taggableBufferIndicator;
        private readonly ILogger logger;

        [ImportingConstructor]
        public LocationTaggerProvider(IIssueLocationStoreAggregator locationStore, 
            IIssueSpanCalculator spanCalculator, 
            ITaggableBufferIndicator taggableBufferIndicator, 
            ILogger logger)
        {
            this.locationStore = locationStore;
            this.spanCalculator = spanCalculator;
            this.taggableBufferIndicator = taggableBufferIndicator;
            this.logger = logger;
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

            // We only want one location tagger per buffer and we don't want it be disposed until
            // it is not being used by any tag aggregators, so we're wrapping it in a SingletonDisposableTaggerManager
            var taggerManager = buffer.Properties.GetOrCreateSingletonProperty(typeof(SingletonDisposableTaggerManager<IIssueLocationTag>),
                () => new SingletonDisposableTaggerManager<IIssueLocationTag>(InternalCreateTagger));

            var tagger = taggerManager.CreateTagger(buffer);
            return tagger as ITagger<T>;
        }

        private ITagger<IIssueLocationTag> InternalCreateTagger(ITextBuffer buffer)
            => new LocationTagger(buffer, locationStore, spanCalculator, logger);
    }
}
