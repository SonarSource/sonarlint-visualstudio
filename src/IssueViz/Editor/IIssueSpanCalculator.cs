/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.Core.Analysis;
using SonarQube.Client;

namespace SonarLint.VisualStudio.IssueVisualization.Editor
{
    public interface IIssueSpanCalculator
    {
        /// <summary>
        /// Returns the text span corresponding to the supplied analysis issue location.
        /// Returns empty if the location line hash is different from the snapshot line hash
        /// Returns null if no textRange is passed
        /// </summary>
        SnapshotSpan? CalculateSpan(ITextRange range, ITextSnapshot currentSnapshot);
    }

    [Export(typeof(IIssueSpanCalculator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class IssueSpanCalculator : IIssueSpanCalculator
    {
        private static SnapshotSpan EmptySpan { get; } = new SnapshotSpan();

        private readonly IChecksumCalculator checksumCalculator;

        public IssueSpanCalculator()
            : this(new ChecksumCalculator())
        {
        }
        
        internal IssueSpanCalculator(IChecksumCalculator checksumCalculator)
        {
            this.checksumCalculator = checksumCalculator;
        }

        public SnapshotSpan? CalculateSpan(ITextRange range, ITextSnapshot currentSnapshot)
        {
            if (range == null)
            {
                return null;
            }

            if (range.StartLine > currentSnapshot.LineCount)
            {
                // Race condition: the line reported in the diagnostic is beyond the end of the file, so presumably
                // the file has been edited while the analysis was being executed
                return EmptySpan;
            }

            // SonarLint issues line numbers are 1-based, spans lines are 0-based
            var startLine = currentSnapshot.GetLineFromLineNumber(range.StartLine - 1);
            var maxLength = currentSnapshot.Length;
            var startPos = startLine.Start.Position + range.StartLineOffset;

            int endPos;
            if (range.EndLine == 0          // Special case : EndLine = 0 means "select whole of the start line, ignoring the offset"
                || startPos > maxLength)    // Defensive : issue start position is beyond the end of the file. Just select the last line.
            {
                startPos = startLine.Start.Position;
                endPos = startLine.Start.Position + startLine.Length;
            }
            else
            {
                endPos = currentSnapshot.GetLineFromLineNumber(range.EndLine - 1).Start.Position + range.EndLineOffset;
                // Make sure the end position isn't beyond the end of the snapshot either
                endPos = Math.Min(maxLength, endPos);
            }

            var start = new SnapshotPoint(currentSnapshot, startPos);
            var end = new SnapshotPoint(currentSnapshot, endPos);
            var snapshotSpan = new SnapshotSpan(start, end);

            // HACK: range.LineHash might be a line-hash or hash of a part of a line, depending on where the data came from.
            // To make the navigability check work we need to handle both cases.
            // See bug #3708.
            if (RangeHasHash(range) &&
                IsHashDifferent(range.LineHash, snapshotSpan.GetText()) &&
                IsHashDifferent(range.LineHash, startLine.GetText()))
            {
                // Out of sync: the range reported in the diagnostic has been edited, so we can no longer calculate the span
                return EmptySpan;
            }

            return snapshotSpan;
        }

        private static bool RangeHasHash(ITextRange range)
            => !string.IsNullOrEmpty(range.LineHash);

        private bool IsHashDifferent(string issueHash, string documentText)
        {
            var documentTextHash = checksumCalculator.Calculate(documentText);

            return documentTextHash != issueHash;
        }
    }
}
