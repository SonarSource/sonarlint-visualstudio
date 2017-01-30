/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */


using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

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
            this.files.Should().ContainKey(file);
        }

        public void AssertFileNotExists(string file)
        {
            this.files.Should().NotContainKey(file);
        }

        public void AssertDirectoryExists(string dir)
        {
            this.directories.Should().Contain(dir);
        }

        public void AssertDirectoryNotExists(string dir)
        {
            this.directories.Should().NotContain(dir);
        }

        public void AssertFileTimestampBefore(string file, long timestamp)
        {
            long current;
            var success = this.files.TryGetValue(file, out current);

            success.Should().BeTrue("File not found " + file);
            current.Should().BeGreaterThan(timestamp);
        }

        public void AssertFileTimestampAfter(string file, long timestamp)
        {
            long current;
            var success = this.files.TryGetValue(file, out current);

            success.Should().BeTrue("File not found " + file);
            current.Should().BeLessThan(timestamp);
        }

        public long GetFileTimestamp(string file)
        {
            long current;
            var success = this.files.TryGetValue(file, out current);

            success.Should().BeTrue("File not found " + file);

            return current;
        }

        public void AssertFileTimestamp(string file, long timestamp)
        {
            long current;
            var success = this.files.TryGetValue(file, out current);

            success.Should().BeTrue("File not found " + file);
            current.Should().Be(timestamp);
        }
        #endregion
    }
}
