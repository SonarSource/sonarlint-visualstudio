/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.LocalServices;

[Export(typeof(ICanonicalFilePathsCache))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class CanonicalFilePathsCache : ICanonicalFilePathsCache
{
    private readonly IActiveSolutionTracker activeSolutionTracker;
    private bool disposed;
    private readonly object lockObject = new();
    private readonly Dictionary<string, string> canonicalPathsCache = new(StringComparer.OrdinalIgnoreCase);

    [method: ImportingConstructor]
    public CanonicalFilePathsCache(IActiveSolutionTracker activeSolutionTracker)
    {
        this.activeSolutionTracker = activeSolutionTracker;
        activeSolutionTracker.ActiveSolutionChanged += ActiveSolutionTracker_ActiveSolutionChanged;
    }

    private void ActiveSolutionTracker_ActiveSolutionChanged(object _, ActiveSolutionChangedEventArgs args)
    {
        if (args.IsSolutionOpen)
        {
            return;
        }

        lock (lockObject)
        {
            canonicalPathsCache.Clear();
        }
    }

    public void Add(IEnumerable<string> files)
    {
        lock (lockObject)
        {
            ThrowIfDisposed();

            foreach (var file in files)
            {
                canonicalPathsCache[file] = file;
            }
        }
    }

    public void Add(string filePath)
    {
        lock (lockObject)
        {
            ThrowIfDisposed();

            canonicalPathsCache[filePath] = filePath;
        }
    }

    public bool TryGet(string filePath, out string canonicalPath)
    {
        lock (lockObject)
        {
            ThrowIfDisposed();

            return canonicalPathsCache.TryGetValue(filePath, out canonicalPath);
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(CanonicalFilePathsCache));
        }
    }

    public void Dispose()
    {
        lock (lockObject)
        {
            if (disposed)
            {
                return;
            }

            activeSolutionTracker.ActiveSolutionChanged -= ActiveSolutionTracker_ActiveSolutionChanged;
            canonicalPathsCache.Clear();
            disposed = true;
        }
    }
}
