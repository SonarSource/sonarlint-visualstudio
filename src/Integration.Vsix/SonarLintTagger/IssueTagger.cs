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
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Flyweight wrapper that provides data and notifications about tags
    /// in a single view to a single consumer
    /// </summary>
    /// <remarks>
    /// See the README.md in this folder for more information
    /// </remarks>
    internal sealed class IssueTagger : ITagger<IErrorTag>, IDisposable
    {
        public delegate void OnTaggerDisposed(IssueTagger issueTagger);

        private IEnumerable<IAnalysisIssueVisualization> issueMarkers;
        private readonly OnTaggerDisposed onTaggerDisposed;
        private bool isDisposed;

        public IssueTagger(IEnumerable<IAnalysisIssueVisualization> issueMarkers, OnTaggerDisposed onTaggerDisposed)
        {
            this.issueMarkers = issueMarkers;
            this.onTaggerDisposed = onTaggerDisposed;
        }

        public void UpdateMarkers(IEnumerable<IAnalysisIssueVisualization> newMarkers, SnapshotSpan? affectedSpan)
        {
            issueMarkers = newMarkers;

            var handler = this.TagsChanged;
            if (handler != null && affectedSpan.HasValue)
            {
                handler(this, new SnapshotSpanEventArgs(affectedSpan.Value));
            }
        }
        
        public void Dispose()
        {
            if (!isDisposed)
            {
                // Called when the tagger is no longer needed (generally when the ITextView is closed).
                onTaggerDisposed?.Invoke(this);
                this.isDisposed = true;
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            Debug.Assert(!isDisposed, "Not expecting GetTags to be called after the tagger has been disposed");

            // TODO: remove this class completely
            // Tagging will be handled by a different set of classes
            return Enumerable.Empty<ITagSpan<IErrorTag>>();
        }
    }
}
