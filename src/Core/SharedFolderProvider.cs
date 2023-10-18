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
using System.IO;
using System.IO.Abstractions;

namespace SonarLint.VisualStudio.Core
{
    public interface ISharedFolderProvider
    {
        /// <summary>
        /// Returns the file path to the current Shared Folder
        /// Or null if there's not a Shared Folder
        /// </summary>
        string GetSharedFolderPath();
    }

    [Export(typeof(ISharedFolderProvider))]
    internal class SharedFolderProvider : ISharedFolderProvider
    {
        private readonly ISolutionInfoProvider solutionInfoProvider;
        private readonly IFileSystem fileSystem;

        private const string sharedFolder = ".sonarlint";

        [ImportingConstructor]
        public SharedFolderProvider(ISolutionInfoProvider solutionInfoProvider) : this(solutionInfoProvider, new FileSystem())
        { }

        internal SharedFolderProvider(ISolutionInfoProvider solutionInfoProvider, IFileSystem fileSystem)
        {
            this.solutionInfoProvider = solutionInfoProvider;
            this.fileSystem = fileSystem;
        }

        public string GetSharedFolderPath()
        {
            var workspaceRoot = solutionInfoProvider.GetSolutionDirectory();
            string sonarlintFolder;

            if (workspaceRoot == null) { return null; }

            var currentDir = new DirectoryInfo(workspaceRoot);

            while (currentDir != null && fileSystem.Directory.Exists(currentDir.FullName))
            {
                sonarlintFolder = Path.Combine(currentDir.FullName, sharedFolder);

                if (fileSystem.Directory.Exists(sonarlintFolder)) { return sonarlintFolder; }

                currentDir = currentDir.Parent;
            }

            return null;
        }
    }
}
