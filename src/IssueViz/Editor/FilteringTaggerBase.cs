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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace SonarLint.VisualStudio.IssueVisualization.Editor
{
    /// <summary>
    /// Base class for a tagger that produces tags that are based on another tag type
    /// e.g. a tagger that produces IErrorTags based on our IIssueLocationTags
    /// </summary>
    /// <typeparam name="TTrackedTagType">The type of tag being tracked</typeparam>
    /// <typeparam name="TTagType">The type of tag being produced</typeparam>
    internal abstract class FilteringTaggerBase<TTrackedTagType, TTagType> : ITagger<TTagType>, IDisposable
        where TTrackedTagType : ITag
        where TTagType : ITag
    {
        private readonly ITagAggregator<TTrackedTagType> tagAggregator;
        private readonly ITextBuffer buffer;
        private readonly ITextView view;
        private bool disposedValue;

        #region Construction

        // View taggers are associated with an ITextView, buffer taggers with an ITextBuffer.
        // This base class has two constructors, one that accepts a view, the other a buffer.        
        // The subclass should call the appropriate constructor, depending on whether it is
        // a view or buffer tagger.

        protected FilteringTaggerBase(ITagAggregator<TTrackedTagType> tagAggregator, ITextBuffer textBuffer)
            : this(tagAggregator)
        {
            this.buffer = textBuffer;
        }

        protected FilteringTaggerBase(ITagAggregator<TTrackedTagType> tagAggregator, ITextView textView)
            : this(tagAggregator)
        {
            this.view = textView;
        }

        private FilteringTaggerBase(ITagAggregator<TTrackedTagType> tagAggregator)
        {
            this.tagAggregator = tagAggregator;
            tagAggregator.BatchedTagsChanged += OnAggregatorTagsChanged;
        }

        #endregion

        /// <summary>
        /// Factory method to create a new tag span object for the specified tracked tag
        /// </summary>
        /// <param name="trackedTag">The tracked tag</param>
        /// <param name="spans">The matching set of spans in the current buffer/view</param>
        protected abstract TagSpan<TTagType> CreateTagSpan(TTrackedTagType trackedTag, NormalizedSnapshotSpanCollection spans);

        /// <summary>
        /// Filters the set of tracked tag spans
        /// </summary>
        /// <remarks>The default implementation does not apply any filtering</remarks>
        protected virtual IEnumerable<IMappingTagSpan<TTrackedTagType>> Filter(IEnumerable<IMappingTagSpan<TTrackedTagType>> trackedTagSpans) =>
            trackedTagSpans;

        /// <summary>
        /// Signals to the editor that the set of tags has changed
        /// </summary>
        protected void NotifyTagsChanged()
        {
            var snapshot = GetSnapshot();

            // Note: we're currently reporting the whole snapshot as having changed. We could be more specific
            // if our buffer raised a more specific notification.
            var wholeSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(wholeSpan));
        }

        /// <summary>
        /// We'll either have a buffer or a view, depending on the type of tagger
        /// </summary>
        protected ITextSnapshot GetSnapshot() => buffer?.CurrentSnapshot ?? view.TextSnapshot;

        /// <summary>
        /// Handles notifications the tag aggregator that the set of tracked tags has changed
        /// </summary>
        private void OnAggregatorTagsChanged(object sender, BatchedTagsChangedEventArgs e)
        {
            // We need to propagate the change notification, otherwise our GetTags method won't be called.

            // Note: we're currently reporting the whole snapshot as having changed. We could be more specific
            // if our buffer raised a more specific notification.
            NotifyTagsChanged();
        }

        #region ITagger implementation

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<TTagType>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (disposedValue)
            {
                // See https://github.com/SonarSource/sonarlint-visualstudio/issues/1693
                // Intermittently GetTags gets called even after the tagger instance has been
                // disposed. We don't want an exception to be thrown in this case as it causes
                // a gold bar to appear. To the user, it would look like the bug is in our code,
                // whereas in fact VS has disposed the tagger but is continuing to call it.
                Debug.Fail($"GetTags called on disposed tagger. Tagger type: {this.GetType().FullName}. File: { GetSnapshot().TextBuffer.GetFilePath()}");
                yield break;
            }

            if (spans.Count == 0) { yield break; }

            var trackedTagSpans = tagAggregator.GetTags(spans).ToArray();
            if (trackedTagSpans.Length == 0) { yield break; }

            var filteredTagSpans = Filter(trackedTagSpans);

            var currentSnapshot = GetSnapshot();
            var callerSnapshot = spans[0].Snapshot;

            if (callerSnapshot.Version != currentSnapshot.Version)
            {
                Debug.Fail($"Expecting the tags to reference the correct snapshot. Caller snapshot version: {callerSnapshot.Version}, tag snapshot version: {currentSnapshot.Version}");
                yield break;
            }

            foreach (var dataTagSpan in filteredTagSpans)
            {
                var filterSpans = dataTagSpan.Span.GetSpans(currentSnapshot);
                if (spans.IntersectsWith(filterSpans))
                {
                    yield return CreateTagSpan(dataTagSpan.Tag, spans);
                }
            }
        }

        #endregion

        #region IDisposable implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    tagAggregator.BatchedTagsChanged -= OnAggregatorTagsChanged;
                    tagAggregator.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
