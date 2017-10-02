/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Xml.Serialization;

namespace SonarLint.VisualStudio.Integration
{
    public sealed class TelemetryDataRepository : ITelemetryDataRepository
    {
        private static readonly string StorageFilePath = GetStorageFilePath();

        private readonly FileSystemWatcher fileWatcher;

        private bool ignoreFileChange;

        public TelemetryData Data { get; private set; } = new TelemetryData { IsAnonymousDataShared = true };

        public TelemetryDataRepository()
        {
            EnsureFileExists(StorageFilePath);
            ReadFromXmlFile();

            this.fileWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(StorageFilePath),
                Filter = Path.GetFileName(StorageFilePath),
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            this.fileWatcher.Changed += OnStorageFileChanged;
        }

        internal static string GetStorageFilePath()
        {
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
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            if (!File.Exists(filePath))
            {
                WriteToXmlFile();
            }
        }

        private void WriteToXmlFile()
        {
            TextWriter writer = null;
            var success = RetryHelper.RetryOnException(3, TimeSpan.FromSeconds(2),
                () =>
                {
                    var serializer = new XmlSerializer(typeof(TelemetryData));
                    writer = new StreamWriter(StorageFilePath, false);
                    this.ignoreFileChange = true;
                    serializer.Serialize(writer, Data);
                    writer.Flush();
                });
            writer?.Close();

            if (!success)
            {
                this.ignoreFileChange = false;
            }
        }

        private void ReadFromXmlFile()
        {
            FileStream stream = null;
            RetryHelper.RetryOnException(3, TimeSpan.FromSeconds(2),
                () =>
                {
                    stream = File.OpenRead(StorageFilePath);
                    var serializer = new XmlSerializer(typeof(TelemetryData));
                    try
                    {
                        this.Data = serializer.Deserialize(stream) as TelemetryData;
                    }
                    catch (InvalidOperationException)
                    {
                        File.Delete(StorageFilePath);
                        EnsureFileExists(StorageFilePath);
                    }
                });
            stream?.Dispose();
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
