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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace SonarLint.VisualStudio.IssueVisualization.Editor
{
    partial class SingletonDisposableTaggerManager<TTagType>
    {
        /// <summary>
        /// Forwards calls/events to the wrapped tagger, and calls the "OnTaggerDisposed" delegate when disposed
        /// </summary>
        internal sealed class FlyweightTaggerWrapper : ITagger<TTagType>, IDisposable
        {
            private readonly ITagger<TTagType> wrappedTagger;
            private readonly OnTaggerDisposed onTaggerDisposed;
            private bool disposedValue;

            public FlyweightTaggerWrapper(ITagger<TTagType> wrappedTagger, OnTaggerDisposed onTaggerDisposed)
            {
                this.wrappedTagger = wrappedTagger;
                this.onTaggerDisposed = onTaggerDisposed;
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged
            {
                add
                {
                    wrappedTagger.TagsChanged += value;
                }

                remove
                {
                    wrappedTagger.TagsChanged -= value;
                }
            }

            public IEnumerable<ITagSpan<TTagType>> GetTags(NormalizedSnapshotSpanCollection spans) => wrappedTagger.GetTags(spans);

            private void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        onTaggerDisposed?.Invoke(this);
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
        }
    }
}
