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

using System.IO;

namespace SonarLint.VisualStudio.Core.SystemAbstractions
{
    public class FileSystemWatcherWrapperFactory : IFileSystemWatcherFactory
    {
        public IFileSystemWatcher Create() => new FileSystemWatcherWrapper();

        private sealed class FileSystemWatcherWrapper : IFileSystemWatcher
        {
            private readonly FileSystemWatcher watcher = new FileSystemWatcher();

            public bool EnableRaisingEvents
            {
                get
                {
                    return watcher.EnableRaisingEvents;
                }
                set
                {
                    watcher.EnableRaisingEvents = value;
                }
            }

            public string Filter
            {
                get
                {
                    return watcher.Filter;
                }
                set
                {
                    watcher.Filter = value;
                }
            }

            public NotifyFilters NotifyFilter
            {
                get
                {
                    return watcher.NotifyFilter;
                }
                set
                {
                    watcher.NotifyFilter = value;
                }
            }

            public string Path
            {
                get
                {
                    return watcher.Path;
                }
                set
                {
                    watcher.Path = value;
                }
            }

            public event FileSystemEventHandler Changed
            {
                add
                {
                    watcher.Changed += value;
                }
                remove
                {
                    watcher.Changed -= value;
                }
            }

            public event FileSystemEventHandler Created
            {
                add
                {
                    watcher.Created += value;
                }
                remove
                {
                    watcher.Created -= value;
                }
            }

            public event FileSystemEventHandler Deleted
            {
                add
                {
                    watcher.Deleted += value;
                }
                remove
                {
                    watcher.Deleted -= value;
                }
            }

            public event RenamedEventHandler Renamed
            {
                add
                {
                    watcher.Renamed += value;
                }
                remove
                {
                    watcher.Renamed -= value;
                }
            }

            public void Dispose() => watcher.Dispose();
        }
    }
}
