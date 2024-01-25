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
using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ETW;
using SonarLint.VisualStudio.Roslyn.Suppressions.Resources;
using SonarLint.VisualStudio.Roslyn.Suppressions.Settings.Cache;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile
{
    /// <summary>
    /// Monitors files created under <see cref="RoslynSettingsFileInfo.Directory"/> directory and
    /// calls <see cref="ISettingsCache.Invalidate"/> when a sonarProject's settings file is changed.
    /// </summary>
    internal interface ISuppressedIssuesFileWatcher : IDisposable
    {
    }

    internal sealed class SuppressedIssuesFileWatcher : ISuppressedIssuesFileWatcher
    {
        private IFileSystemWatcher fileSystemWatcher;
        private readonly ISettingsCache settingsCache;
        private readonly ILogger logger;

        public SuppressedIssuesFileWatcher(ISettingsCache settingsCache, ILogger logger)
            : this(settingsCache, logger, new FileSystem())
        {
        }

        internal SuppressedIssuesFileWatcher(ISettingsCache settingsCache, ILogger logger, IFileSystem fileSystem)
        {
            this.settingsCache = settingsCache;
            this.logger = logger;

            WatchSuppressionsDirectory(fileSystem);
        }

        /// <summary>
        /// Monitors files created/edited/deleted in <see cref="RoslynSettingsFileInfo.Directory"/> folder.
        /// </summary>
        /// <remarks>
        /// This method can throw (for example if the directory does not exist yet).
        /// It's a deliberate design choice to not catch exceptions and let the caller handle it.
        /// This is because parts of the system need to be singletons and the caller should be the one responsible for handling that.
        /// </remarks>
        private void WatchSuppressionsDirectory(IFileSystem fileSystem)
        {
            fileSystemWatcher = fileSystem.FileSystemWatcher.FromPath(RoslynSettingsFileInfo.Directory);

            fileSystemWatcher.Filter = "*.json";
            fileSystemWatcher.Created += InvalidateCache;
            fileSystemWatcher.Changed += InvalidateCache;
            fileSystemWatcher.Deleted += InvalidateCache;

            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void InvalidateCache(object sender, FileSystemEventArgs e)
        {
            try
            {
                CodeMarkers.Instance.FileWatcherInvalidateStart(e.ChangeType.ToString());
                var fileName = e.Name;
                var settingsKey = RoslynSettingsFileInfo.GetSettingsKey(fileName);

                if (!string.IsNullOrEmpty(fileName))
                {
                    settingsCache.Invalidate(settingsKey);
                }
            }
            catch (Exception ex)
            {
                logger.WriteLine(Strings.FileWatcherException, ex);
            }
            finally
            {
                CodeMarkers.Instance.FileWatcherInvalidateStop(e.ChangeType.ToString());
            }
        }

        public void Dispose()
        {
            fileSystemWatcher.Created -= InvalidateCache;
            fileSystemWatcher.Changed -= InvalidateCache;
            fileSystemWatcher.Deleted -= InvalidateCache;
            fileSystemWatcher.Dispose();
        }
    }
}
