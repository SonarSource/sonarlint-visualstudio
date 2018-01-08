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
using System.Linq;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableFileSystem : IFileSystem
    {
        internal readonly HashSet<string> directories =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal readonly Dictionary<string, long> files =
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        #region IFileSystem

        void IFileSystem.CreateDirectory(string path)
        {
            this.directories.Add(path);
        }

        bool IFileSystem.DirectoryExists(string path)
        {
            return this.directories.Contains(path);
        }

        bool IFileSystem.FileExist(string filePath)
        {
            return this.files.ContainsKey(filePath);
        }

        #endregion IFileSystem

        #region Test helpers

        public void ClearDirectories()
        {
            this.directories.Clear();
        }

        public void ClearFiles()
        {
            this.files.Clear();
        }

        public void RegisterDirectories(params string[] dirs)
        {
            directories.UnionWith(dirs);
        }

        public void RegisterFiles(params string[] newFiles)
        {
            newFiles.ToList().ForEach(this.RegisterFile);
        }

        public void RegisterFile(string file)
        {
            this.UpdateTimestamp(file);
        }

        public void UpdateTimestamp(string file)
        {
            this.SetTimestamp(file, DateTime.UtcNow.Ticks);
        }

        public void SetTimestamp(string file, long timestamp)
        {
            this.files[file] = timestamp;
        }

        public long GetFileTimestamp(string file)
        {
            long current;
            var isFound = this.files.TryGetValue(file, out current);

            isFound.Should().BeTrue("File not found " + file);
            return current;
        }

        public void AssertFileTimestamp(string file, long timestamp)
        {
            long current;
            var isFound = this.files.TryGetValue(file, out current);

            isFound.Should().BeTrue("File not found " + file);
            current.Should().Be(timestamp, $"Expected {timestamp} == {current}");
        }

        #endregion Test helpers
    }
}