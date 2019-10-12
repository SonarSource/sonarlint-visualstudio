/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    internal class UserSettingFileChangeHandler
    {
        private readonly IFileSystemWatcher fileWatcher;
        private readonly ILogger logger;

        public UserSettingFileChangeHandler(ILogger logger)
            : this(new FileSystemWatcherWrapperFactory(), logger)
        {
        }

        public UserSettingFileChangeHandler(IFileSystemWatcherFactory factory, ILogger logger)
        {
            fileWatcher = factory.Create();
            var filePath = UserSettings.UserSettingsFilePath;
            fileWatcher.Path = Path.GetDirectoryName(filePath);
            fileWatcher.Filter = Path.GetFileName(filePath);
            fileWatcher.NotifyFilter = System.IO.NotifyFilters.CreationTime | System.IO.NotifyFilters.LastWrite |
                NotifyFilters.FileName | NotifyFilters.DirectoryName;

            fileWatcher.EnableRaisingEvents = true;
            fileWatcher.Changed += OnSettingsFileChanged;
            fileWatcher.Created += OnSettingsFileChanged;
            fileWatcher.Deleted += OnSettingsFileChanged;
            fileWatcher.Renamed += (s, e) => OnSettingsFileChanged(s, e);

            this.logger = logger;
        }

        private static int counter = 0;

        private DateTime lastWriteTime = DateTime.MinValue;

        private void OnSettingsFileChanged(object sender, System.IO.FileSystemEventArgs e)
        {
            // Duplicate events can be raised. See https://stackoverflow.com/questions/1764809/filesystemwatcher-changed-event-is-raised-twice
            var currentWriteTime = File.GetLastWriteTimeUtc(e.FullPath);

            if (currentWriteTime != lastWriteTime)
            {
                // TODO:

                try
                {
                    fileWatcher.EnableRaisingEvents = false;
                    logger.WriteLine($"Settings file changed: {++counter}. Change type: {e.ChangeType}");

                    System.Threading.Thread.Sleep(100);
                    lastWriteTime = currentWriteTime;
                }
                finally
                {
                    fileWatcher.EnableRaisingEvents = true;
                }
            }
            else
            {
                logger.WriteLine($"Discarding duplicate event: {++counter}. Change type:{e.ChangeType}");
            }
        }
    }
}
