/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.IO.Abstractions;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    [Export(typeof(IVsAwareFileSystem))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class VsAwareFileSystem : IVsAwareFileSystem
    {
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        [ImportingConstructor]
        public VsAwareFileSystem(ILogger logger)
            : this(logger, new FileSystem())
        { }

        internal /* for testing */ VsAwareFileSystem(ILogger logger, IFileSystem fileSystem)
        {
            this.logger = logger;
            this.fileSystem = fileSystem;
        }

        public Task DeleteFolderAsync(string folderPath)
        {
            // TODO - error handling
            LogVerbose($"Deleting directory: {folderPath}");
            fileSystem.Directory.Delete(folderPath, true);
            return Task.CompletedTask;
        }

        public Task<string> LoadAsTextAsync(string filePath)
        {
            // TODO - error handling
            logger.LogVerbose($"Reading file: {filePath}");
            var content = fileSystem.File.ReadAllText(filePath);
            return Task.FromResult(content);
        }

        public Task SaveAsync(string filePath, string text)
        {
            // TODO - error handling
            logger.LogVerbose($"Saving file: {filePath}");
            fileSystem.File.WriteAllText(filePath, text);
            return Task.CompletedTask;
        }

        private void LogVerbose(string message) => logger.LogVerbose("[Migration] " + message);
    }
}
