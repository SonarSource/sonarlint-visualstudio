﻿/*
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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Adornment
{
    internal sealed class IssueLocationAdornmentTagger : FilteringTaggerBase<ISelectedIssueLocationTag, IntraTextAdornmentTag>
    {
        private readonly IWpfTextView wpfView;
        private readonly ICachingAdornmentFactory adornmentCache;
        private readonly ITagAggregator<ISelectedIssueLocationTag> tagAggregator;

        public IssueLocationAdornmentTagger(ITagAggregator<ISelectedIssueLocationTag> tagAggregator, IWpfTextView textView)
            : this(tagAggregator, textView, new CachingAdornmentFactory(textView))
        {
        }

        internal /* for testing */ IssueLocationAdornmentTagger(ITagAggregator<ISelectedIssueLocationTag> tagAggregator,
            IWpfTextView wpfView, ICachingAdornmentFactory adornmentCache)
            : base(tagAggregator, wpfView)
        {
            this.wpfView = wpfView;
            this.adornmentCache = adornmentCache;

            this.tagAggregator = tagAggregator;
            tagAggregator.BatchedTagsChanged += OnTagsChanged;
        }

        private void OnTagsChanged(object sender, BatchedTagsChangedEventArgs e)
        {
            // Remove any unnecessary adornments from the cache. To do that we need to ask the aggregator
            // for all of its tags.
            var wholeSpan = new SnapshotSpan(wpfView.TextSnapshot, 0, wpfView.TextSnapshot.Length);
            var allTags = tagAggregator.GetTags(wholeSpan);
            adornmentCache.RemoveUnused(allTags.Select(x => x.Tag.Location));
        }

        protected override TagSpan<IntraTextAdornmentTag> CreateTagSpan(ISelectedIssueLocationTag trackedTag, NormalizedSnapshotSpanCollection spans)
        {
            // To produce adornments that don't obscure the text, the adornment tags
            // should have zero length spans. Overriding this method allows control
            // over the tag spans.
            var adornmentSpan = new SnapshotSpan(trackedTag.Location.Span.Value.Start, 0);
            var adornment = adornmentCache.CreateOrUpdate(trackedTag.Location);

            return new TagSpan<IntraTextAdornmentTag>(adornmentSpan, new IntraTextAdornmentTag(adornment, null, PositionAffinity.Predecessor));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                tagAggregator.BatchedTagsChanged -= OnTagsChanged;
            }
            base.Dispose(disposing);
        }

        #region Adornment cache

        internal interface ICachingAdornmentFactory
        {
            /// <summary>
            /// Clears unnecessary entries from the cache
            /// </summary>
            /// <param name="currentLocations">The list of locations that are still in use</param>
            void RemoveUnused(IEnumerable<IAnalysisIssueLocationVisualization> currentLocations);

            IssueLocationAdornment CreateOrUpdate(IAnalysisIssueLocationVisualization location);
        }

        internal class CachingAdornmentFactory : ICachingAdornmentFactory
        {
            private readonly IWpfTextView wpfTextView;
            private readonly IDictionary<IAnalysisIssueLocationVisualization, IssueLocationAdornment> locVizToAdornmentMap;

            internal /* for testing */ IReadOnlyCollection<IssueLocationAdornment> CachedAdornments => locVizToAdornmentMap.Values.ToList();

            public CachingAdornmentFactory(IWpfTextView view)
            {
                wpfTextView = view;
                locVizToAdornmentMap = new Dictionary<IAnalysisIssueLocationVisualization, IssueLocationAdornment>();
            }

            public IssueLocationAdornment CreateOrUpdate(IAnalysisIssueLocationVisualization locationViz)
            {
                // As long as the cache is always accessed on the same thread we don't need to worry
                // about synchronising access to it. Currently VS is always running this code on the
                // main thread. The assertion is to detect if this behaviour changes.
                Debug.Assert(ThreadHelper.CheckAccess(), "Expected cache to be accessed on the UI thread");

                if (locVizToAdornmentMap.TryGetValue(locationViz, out var existingAdornment))
                {
                    existingAdornment.Update(wpfTextView.FormattedLineSource);
                    return existingAdornment;
                }

                var newAdornment = CreateAdornment(locationViz);
                locVizToAdornmentMap[locationViz] = newAdornment;
                return newAdornment;
            }

            public void RemoveUnused(IEnumerable<IAnalysisIssueLocationVisualization> currentLocations)
            {
                Debug.Assert(ThreadHelper.CheckAccess(), "Expected cache to be accessed on the UI thread");

                var unused = locVizToAdornmentMap.Keys.Except(currentLocations).ToArray();
                foreach(var item in unused)
                {
                    locVizToAdornmentMap.Remove(item);
                }
            }

            private IssueLocationAdornment CreateAdornment(IAnalysisIssueLocationVisualization locationViz)
            {
                // To produce adornments that don't obscure the text, the adornment tags
                // should have zero length spans. Overriding this method allows control
                // over the tag spans.
                var adornmentSpan = new SnapshotSpan(locationViz.Span.Value.Start, 0);
                var adornment = new IssueLocationAdornment(locationViz, wpfTextView.FormattedLineSource);

                // If we don't call Measure here the tag is positioned incorrectly
                adornment.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                return adornment;
            }
        }

        #endregion Adornment cache
    }
}
