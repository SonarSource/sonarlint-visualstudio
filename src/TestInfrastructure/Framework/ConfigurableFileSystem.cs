//-----------------------------------------------------------------------
// <copyright file="ConfigurableFileSystem.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableFileSystem : IFileSystem
    {
        private readonly HashSet<string> directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, long> files = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

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

        public void AssertFileExists(string file)
        {
            Assert.IsTrue(this.files.ContainsKey(file), "File not exists: " + file);
        }

        public void AssertFileNotExists(string file)
        {
            Assert.IsFalse(this.files.ContainsKey(file), "File exists: " + file);
        }

        public void AssertDirectoryExists(string dir)
        {
            Assert.IsTrue(this.directories.Contains(dir), "Directory not exists: " + dir);
        }

        public void AssertDirectoryNotExists(string dir)
        {
            Assert.IsFalse(this.directories.Contains(dir), "Directory exists: " + dir);
        }

        public void AssertFileTimestampBefore(string file, long timestamp)
        {
            long current;
            if (this.files.TryGetValue(file, out current))
            {
                Assert.IsTrue(timestamp < current, $"Expected {timestamp} < {current}");
            }
            else
            {
                Assert.Fail("File not found " + file);
            }
        }

        public void AssertFileTimestampAfter(string file, long timestamp)
        {
            long current;
            if (this.files.TryGetValue(file, out current))
            {
                Assert.IsTrue(timestamp > current, $"Expected {timestamp} > {current}");
            }
            else
            {
                Assert.Fail("File not found " + file);
            }
        }

        public long GetFileTimestamp(string file)
        {
            long current;
            if (this.files.TryGetValue(file, out current))
            {
                return current;
            }
            else
            {
                Assert.Fail("File not found " + file);
                return -1;
            }
        }

        public void AssertFileTimestamp(string file, long timestamp)
        {
            long current;
            if (this.files.TryGetValue(file, out current))
            {
                Assert.AreEqual(timestamp, current, $"Expected {timestamp} == {current}");
            }
            else
            {
                Assert.Fail("File not found " + file);
            }
        }
        #endregion
    }
}
