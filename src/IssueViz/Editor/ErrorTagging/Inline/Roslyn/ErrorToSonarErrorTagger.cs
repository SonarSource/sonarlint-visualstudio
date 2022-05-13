/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.Text.RegularExpressions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.TableControls;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging.Inline.Roslyn
{
    internal sealed class ErrorToSonarErrorTagger : ITagger<ISonarErrorTag>, IDisposable
    {
        private readonly ITextBuffer buffer;
        private readonly ISonarAndRoslynErrorsProvider sonarAndRoslynErrorsProvider;
        private readonly ILogger logger;

        internal IList<ITagSpan<ISonarErrorTag>> TagSpans { get; private set; }

        public ErrorToSonarErrorTagger(ITextBuffer buffer, ISonarAndRoslynErrorsProvider sonarAndRoslynErrorsProvider, ILogger logger)
        {
            this.buffer = buffer;
            this.sonarAndRoslynErrorsProvider = sonarAndRoslynErrorsProvider;
            this.logger = logger;

            // todo: need to call UpdateTags but we don't have yet the table entries
            TagSpans = new List<ITagSpan<ISonarErrorTag>>();;

            sonarAndRoslynErrorsProvider.IssuesChanged += OnIssuesChanged;
            buffer.ChangedLowPriority += SafeOnBufferChange;
        }

        /// <summary>
        /// We're not tracking file renames so we have to fetch the current
        /// file name each time we need it.
        /// </summary>
        internal /* for testing */ string GetCurrentFilePath() => buffer.GetFilePath();

        private void OnIssuesChanged(object sender, IssuesChanged e)
        {
            var filePath = buffer.GetFilePath();
            var entries = e.Factory.GetCurrentSnapshot();

            for (var i = 0; i < entries.Count; i++)
            {
                if (ShouldGenerateTag(entries, i, filePath))
                {
                    UpdateTags(entries);
                    break;
                }
            }
        }

        private static bool ShouldGenerateTag(ITableEntriesSnapshot entries, int i, string filePath) =>
            IsSonarError(entries, i) && IsErrorForThisFile(entries, i, filePath);

        private void UpdateTags(ITableEntriesSnapshot tableEntriesSnapshot)
        {
            var textSnapshot = buffer.CurrentSnapshot;
            var oldTags = TagSpans;
            TagSpans = CreateTagSpans(tableEntriesSnapshot, textSnapshot);

            var affectedSpan = CalculateSpanOfAllTags(oldTags, textSnapshot);
            NotifyTagsChanged(affectedSpan);
        }

        private static List<ITagSpan<ISonarErrorTag>> CreateTagSpans(ITableEntriesSnapshot entries, ITextSnapshot textSnapshot)
        {
            var tagSpans = new List<ITagSpan<ISonarErrorTag>>();
            var filePath = textSnapshot.TextBuffer.GetFilePath();

            for (var i = 0; i < entries.Count; i++)
            {
                if (ShouldGenerateTag(entries, i, filePath))
                {
                    var tag = CreateTagSpan(entries, i, textSnapshot);
                    tagSpans.Add(tag);
                }
            }

            return tagSpans;
        }

        private static ITagSpan<ISonarErrorTag> CreateTagSpan(ITableEntriesSnapshot entries, int index, ITextSnapshot textSnapshot)
        {
            entries.TryGetValue(index, StandardTableKeyNames.ErrorCode, out var errorCode);
            entries.TryGetValue(index, StandardTableKeyNames.DocumentName, out var fileName);
            entries.TryGetValue(index, StandardTableKeyNames.Text, out var message);
            entries.TryGetValue(index, StandardTableKeyNames.Line, out var startLineObj);
            var startLine = Convert.ToInt32(startLineObj);

            var issue = new AnalysisIssue(errorCode as string,
                AnalysisIssueSeverity.Info,
                AnalysisIssueType.CodeSmell,
                new AnalysisIssueLocation(message as string, fileName as string,
                    new TextRange(startLine,
                        startLine,
                        0,
                        0,
                        null)),
                null, null);

            var issueLine = textSnapshot.GetLineFromLineNumber(startLine);
            var span = new Span(issueLine.Start, issueLine.Length);
            var snapshotSpan = new SnapshotSpan(textSnapshot, span);

            var issueViz = new AnalysisIssueVisualization(null, issue, snapshotSpan, null);
            
            return new TagSpan<ISonarErrorTag>(snapshotSpan, new SonarErrorTag(issueViz));
        }

        #region ITagger implementation

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<ISonarErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || TagSpans.Count == 0) { yield break; }

            // If the requested snapshot isn't the same as our tags are on, translate our spans to the expected snapshot 
            if (spans[0].Snapshot != TagSpans[0].Span.Snapshot)
            {
                TranslateTagSpans(spans[0].Snapshot);
            }

            Debug.Assert(TagSpans.All(span => span.Tag.IssueViz.IsNavigable()), "Expecting all tags would be navigable");
            // Find any tags in that overlap with that range
            foreach (var tagSpan in TagSpans)
            {
                if (IntersectionExists(tagSpan.Span, spans))
                {
                    yield return tagSpan;
                }
            }
        }

        private void TranslateTagSpans(ITextSnapshot newSnapshot)
        {
            var translatedTagSpans = new List<ITagSpan<ISonarErrorTag>>();

            foreach (var old in TagSpans)
            {
                try
                {
                    if (!old.Tag.IssueViz.Span.IsNavigable())
                    {
                        continue;
                    }

                    var newSpan = old.Span.TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive);
                    var hasSpanBeenEdited = newSpan.Length != old.Span.Length;

                    if (hasSpanBeenEdited)
                    {
                        // If the user has typed inside the tagged region we'll stop showing a Tag for that span
                        old.Tag.IssueViz.InvalidateSpan();
                    }
                    else
                    {
                        old.Tag.IssueViz.Span = newSpan;
                        translatedTagSpans.Add(new TagSpan<ISonarErrorTag>(newSpan, old.Tag));
                    }
                }
                catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                {
                    logger.WriteLine($"Failed to translate tag span for file `{GetCurrentFilePath()}`: {ex}");
                }
            }

            TagSpans = translatedTagSpans;
        }

        private static bool IntersectionExists(Span span, NormalizedSnapshotSpanCollection spans) =>
            // We're using "IntersectsWith" rather than "OverlapsWith" so that zero-length spans are handled correctly.
            // Given two spans A = [10 -> 15] and B = [12 -> 12],
            // * A.IntersectsWith(B) == true
            // * A.OverlapsWith(B) == false.
            // This matters because when VS is requesting tags to show tooltips it uses a zero-length span.
            spans.Any(x => x.IntersectsWith(span));

        #endregion ITagger implementation

        #region Buffer change tracking

        private void SafeOnBufferChange(object sender, TextContentChangedEventArgs e)
        {
            // Handles callback from VS. Suppress non-critical errors to prevent them
            // propagating to VS, which would display a dialogue and disable the extension.
            try
            {
                // The text buffer has been edited (i.e.text added, deleted or modified).
                // The spans we have stored for location relate to the previous text buffer and
                // are no longer valid, so we need to translate them to the equivalent spans
                // in the new text buffer.
                var newTextSnapshot = e.After;
                TranslateTagSpans(newTextSnapshot);

                var affectedSpan = CalculateSpanOfChanges(e.Changes, newTextSnapshot);
                NotifyTagsChanged(affectedSpan);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.ERR_HandlingBufferChange, ex.Message);
            }
        }

        private void NotifyTagsChanged(SnapshotSpan affectedSpan)
        {
            // Notify the editor that our set of tags has changed
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(affectedSpan));
        }

        /// <summary>
        /// Method calculates the span from the start of (old+new) TagSpans and until the end of (old+new) TagSpans
        /// </summary>
        private SnapshotSpan CalculateSpanOfAllTags(IList<ITagSpan<ISonarErrorTag>> oldTags, ITextSnapshot textSnapshot)
        {
            var allTagSpans = TagSpans
                .Union(oldTags ?? Array.Empty<ITagSpan<ISonarErrorTag>>())
                .Select(x => x.Span.Span)
                .ToArray();

            return CalculateAffectedSpan(textSnapshot, allTagSpans, allTagSpans);
        }

        /// <summary>
        /// Method calculates the span from the start of the editor changes and until the end of TagSpans
        /// </summary>
        private SnapshotSpan CalculateSpanOfChanges(INormalizedTextChangeCollection changeCollection, ITextSnapshot newTextSnapshot)
        {
            var startSpans = changeCollection.Select(x => x.NewSpan).ToArray();
            var endSpans = TagSpans.Select(x => x.Span.Span);

            return CalculateAffectedSpan(newTextSnapshot, startSpans, endSpans);
        }

        private static SnapshotSpan CalculateAffectedSpan(ITextSnapshot textSnapshot, IEnumerable<Span> startSpans, IEnumerable<Span> endSpans)
        {
            var start = startSpans.Select(x => x.Start).DefaultIfEmpty().Min();
            var end = endSpans.Select(x => x.End).DefaultIfEmpty(textSnapshot.Length).Max();

            return start < end
                ? new SnapshotSpan(textSnapshot, Span.FromBounds(start, end))
                : new SnapshotSpan(textSnapshot, Span.FromBounds(start, textSnapshot.Length));
        }

        #endregion

        public void Dispose()
        {
            sonarAndRoslynErrorsProvider.IssuesChanged -= OnIssuesChanged;
            buffer.ChangedLowPriority -= SafeOnBufferChange;
        }

        private static bool IsErrorForThisFile(ITableEntriesSnapshot entries, int i, string filePath)
        {
            return entries.TryGetValue(i, StandardTableKeyNames.DocumentName, out var displayPath) &&
                   (displayPath as string).Equals(filePath);
        }

        private static bool IsSonarError(ITableEntriesSnapshot entries, int i)
        {
            var isRoslynIssue = entries.TryGetValue(i, StandardTableKeyNames.ErrorCode, out var errorCode) &&
                                Regex.IsMatch(errorCode as string, "^S.+?");

            var isSonarIssue = entries.TryGetValue(i, SonarLintTableControlConstants.IssueVizColumnName, out _);

            return isRoslynIssue || isSonarIssue;
        }
    }
}
