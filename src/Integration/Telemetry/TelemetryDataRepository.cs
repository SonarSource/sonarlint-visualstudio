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
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Xml.Serialization;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(ITelemetryDataRepository)), PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class TelemetryDataRepository : ITelemetryDataRepository, IDisposable
    {
        private static readonly string StorageFilePath = GetStorageFilePath();

        private readonly IFileSystemWatcher fileWatcher;
        private readonly XmlSerializer telemetrySerializer = new XmlSerializer(typeof(TelemetryData));

        private bool ignoreFileChange;
        private readonly IFileSystem fileSystem;

        public TelemetryData Data { get; private set; } = new TelemetryData { IsAnonymousDataShared = true };

        public TelemetryDataRepository()
            : this(new FileSystem(), new FileSystemWatcherFactory())
        {
        }

        public TelemetryDataRepository(IFileSystem fileSystem, IFileSystemWatcherFactory fileSystemWatcherFactory)
        {
            this.fileSystem = fileSystem;

            EnsureFileExists(StorageFilePath);
            ReadFromXmlFile();

            fileWatcher = fileSystemWatcherFactory.CreateNew();
            fileWatcher.Path = Path.GetDirectoryName(StorageFilePath);
            fileWatcher.Filter = Path.GetFileName(StorageFilePath);
            fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            fileWatcher.EnableRaisingEvents = true;
            fileWatcher.Changed += OnStorageFileChanged;
        }

        internal static string GetStorageFilePath()
        {
            // Note: the data is stored in the roaming profile so it will be sync across machines
            // for domain-joined users.
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var filePath = Path.Combine(appDataFolder, "SonarLint for Visual Studio", "telemetry.xml");
            return Path.GetFullPath(filePath); // get rid of the .. in file path
        }

        private void OnStorageFileChanged(object sender, FileSystemEventArgs e)
        {
            if (this.ignoreFileChange)
            {
                this.ignoreFileChange = false;
            }
            else
            {
                ReadFromXmlFile();
            }
        }

        private void EnsureFileExists(string filePath)
        {
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!fileSystem.Directory.Exists(directoryPath))
            {
                fileSystem.Directory.CreateDirectory(directoryPath);
            }
            if (!fileSystem.File.Exists(filePath))
            {
                WriteToXmlFile();
            }
        }

        private void WriteToXmlFile()
        {
            var success = RetryHelper.RetryOnException(3, TimeSpan.FromSeconds(2),
                () =>
                {
                    ignoreFileChange = true;
                    var stringBuilder = new StringBuilder();
                    telemetrySerializer.Serialize(new StringWriter(stringBuilder), Data);
                    fileSystem.File.WriteAllText(StorageFilePath, stringBuilder.ToString());
                });

            if (!success)
            {
                ignoreFileChange = false;
            }
        }

        private void ReadFromXmlFile()
        {
            RetryHelper.RetryOnException(3, TimeSpan.FromSeconds(2),
                () =>
                {
                    try
                    {
                        var fileContent = fileSystem.File.ReadAllText(StorageFilePath);
                        this.Data = telemetrySerializer.Deserialize(new StringReader(fileContent)) as TelemetryData;
                    }
                    catch (InvalidOperationException)
                    {
                        fileSystem.File.Delete(StorageFilePath);
                        EnsureFileExists(StorageFilePath);
                    }
                });
        }

        public void Save()
        {
            WriteToXmlFile();
        }

        public void Dispose()
        {
            this.fileWatcher.Changed -= OnStorageFileChanged;
            this.fileWatcher.Dispose();
        }
    }
}
