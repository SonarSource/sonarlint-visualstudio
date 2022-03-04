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
using System.IO;
using System.IO.Abstractions;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile
{
    internal interface ISuppressedIssuesCache
    {
        void Invalidate(string settingsKey);
    }

    /// <summary>
    /// Monitors files created under <see cref="RoslynSettingsFileInfo.Directory"/> directory and
    /// calls <see cref="ISuppressedIssuesCache.Invalidate"/> when a sonarProject's settings file is changed.
    /// </summary>
    internal interface ISuppressedIssuesFileWatcher : IDisposable
    {
    }

    internal sealed class SuppressedIssuesFileWatcher : ISuppressedIssuesFileWatcher
    {
        private readonly IFileSystemWatcher fileSystemWatcher;
        private readonly ISuppressedIssuesCache suppressedIssuesCache;

        public SuppressedIssuesFileWatcher(ISuppressedIssuesCache suppressedIssuesCache)
            : this(suppressedIssuesCache, new FileSystem())
        {
        }

        internal SuppressedIssuesFileWatcher(ISuppressedIssuesCache suppressedIssuesCache, IFileSystem fileSystem)
        {
            this.suppressedIssuesCache = suppressedIssuesCache;

            fileSystem.Directory.CreateDirectory(RoslynSettingsFileInfo.Directory);
            fileSystemWatcher = fileSystem.FileSystemWatcher.FromPath(RoslynSettingsFileInfo.Directory);

            fileSystemWatcher.Created += InvalidateCache;
            fileSystemWatcher.Changed += InvalidateCache;
            fileSystemWatcher.Deleted += InvalidateCache;

            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void InvalidateCache(object sender, FileSystemEventArgs e)
        {
            var fileName = e.Name;
            var settingsKey = RoslynSettingsFileInfo.GetSettingsKey(fileName);
            suppressedIssuesCache.Invalidate(settingsKey);
        }

        public void Dispose()
        {
            fileSystemWatcher.Created -= InvalidateCache;
            fileSystemWatcher.Changed -= InvalidateCache;
            fileSystemWatcher.Deleted -= InvalidateCache;
            fileSystemWatcher?.Dispose();
        }
    }
}
