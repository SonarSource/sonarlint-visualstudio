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

namespace SonarLint.VisualStudio.IssueVisualization.Editor
{
    /// <summary>
    /// Ensures that only one instance of a disposable tagger exists for a per buffer at a time.
    /// </summary>
    /// <remarks>When to use - for a tagger type that is:
    /// 1) a singleton-per-buffer
    /// 2) disposable, and
    /// 3) used by tag aggregators.
    ///
    /// Tag aggregrators will call Dispose on any disposable tagger they create, which is a problem
    /// if we really want a disposable tagger to be a per-buffer singleton (we can't just use 
    /// buffer.Properties.GetOrCreateSingletonProperty(...) since an aggregator will still dispose
    /// the tagger, so we end up with a disposed tagger stored in the collection).
    ///
    /// This class works round the problem by the wrapping the "singleton" tagger. All requests for a new tagger
    /// instance will be handed a new flyweight instance that wraps the singleton tagger. The flyweights can
    /// safely be disposed by tag aggregators. This class reference-counts the number of active taggers and
    /// disposes the singleton tagger when there are no remaining active flyweight taggers.
    /// </remarks>
    /// <typeparam name="TTagType">The type of tag returned by the tagger</typeparam>
    public partial class SingletonDisposableTaggerManager<TTagType> where TTagType : ITag
    {
        public delegate ITagger<TTagType> CreateSingleton(ITextBuffer buffer);

        internal delegate void OnTaggerDisposed(ITagger<TTagType> taggerAdapter);

        private readonly HashSet<object> taggerAdapters;
        private readonly CreateSingleton createTagger;

        internal /* for testing */ IReadOnlyList<object> ActiveTaggers => taggerAdapters.ToList();
        internal /* for testing */ ITagger<TTagType> Singleton { get; private set; }

        public SingletonDisposableTaggerManager(CreateSingleton createTagger)
        {
            this.createTagger = createTagger;
            taggerAdapters = new HashSet<object>();
        }

        public ITagger<TTagType> CreateTagger(ITextBuffer buffer)
        {
            if (taggerAdapters.Count == 0)
            {
                Singleton = createTagger(buffer);
                Debug.WriteLine($"SLVS SingletonDisposableTaggerManager ({this.GetHashCode()}): Created new singleton tagger: {Singleton.GetType().Name}");
            }

            var tagger = new FlyweightTaggerWrapper(Singleton, RemoveTagger);
            taggerAdapters.Add(tagger);
            Debug.WriteLine($"SLVS SingletonDisposableTaggerManager ({this.GetHashCode()}): Created new flyweight tagger. New count: {taggerAdapters.Count}. Singleton: {Singleton.GetType().Name}, hash code: {Singleton.GetHashCode()}");

            return tagger;
        }

        private void RemoveTagger(ITagger<TTagType> tagger)
        {
            taggerAdapters.Remove(tagger);

            Debug.WriteLine($"SLVS SingletonDisposableTaggerManager ({this.GetHashCode()}): Flyweight tagger removed. Remaining count: {taggerAdapters.Count}. Singleton: {Singleton.GetType().Name}, hash code: {Singleton.GetHashCode()}");
            if (taggerAdapters.Count == 0)
            {
                if (Singleton is IDisposable disposable)
                {
                    Debug.WriteLine($"SLVS SingletonDisposableTaggerManager ({this.GetHashCode()}): Disposed singleton tagger: {Singleton.GetType().Name}");
                    disposable.Dispose();
                }
                Singleton = null;
            }
        }
    }
}
