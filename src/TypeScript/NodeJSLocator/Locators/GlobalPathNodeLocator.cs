/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.TypeScript.NodeJSLocator.Locators
{
    internal interface IGlobalPathNodeLocator : INodeLocator
    {
    }

    [Export(typeof(IGlobalPathNodeLocator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class GlobalPathNodeLocator : IGlobalPathNodeLocator
    {
        private const string FileName = "node.exe";

        private readonly IFileSystem fileSystem;
        private readonly ILogger logger;

        [ImportingConstructor]
        public GlobalPathNodeLocator(ILogger logger)
            : this(new FileSystem(), logger)
        {
        }

        internal GlobalPathNodeLocator(IFileSystem fileSystem, ILogger logger)
        {
            this.fileSystem = fileSystem;
            this.logger = logger;
        }

        public string Locate()
        {
            var nodeExePath = GetPathToNodeExecutableFromEnvironment();

            if (!string.IsNullOrEmpty(nodeExePath))
            {
                logger.WriteLine(Resources.INFO_FoundInGlobalPath, nodeExePath);
                return nodeExePath;
            }

            logger.WriteLine(Resources.ERR_NotFoundInGlobalPath);
            return null;
        }

        /// <summary>
        /// Copied from MS nodejstools repo: https://github.com/microsoft/nodejstools/blob/275e85d5cd95cad9122f59a76f9e49bead66101b/Nodejs/Product/Nodejs/Nodejs.cs#L112
        /// </summary>
        /// <remarks>
        /// Copied as-is, except replacing `File.Exists` with <see cref="fileSystem.File.Exists"/> for testability
        /// </remarks>
        private string GetPathToNodeExecutableFromEnvironment()
        {
            // If we didn't find node.js in the registry we should look at the user's path.
            foreach (var dir in Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator))
            {
                try
                {
                    var execPath = Path.Combine(dir, FileName);

                    if (fileSystem.File.Exists(execPath))
                    {
                        return execPath;
                    }
                }
                catch (ArgumentException) { /*noop*/ }
            }

            // It wasn't in the users path.  Check Program Files for the nodejs folder.
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", FileName);
            
            if (fileSystem.File.Exists(path))
            {
                return path;
            }

            // It wasn't in the users path.  Check Program Files x86 for the nodejs folder.
            var x86path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            
            if (!string.IsNullOrEmpty(x86path))
            {
                path = Path.Combine(x86path, "nodejs", FileName);
                
                if (fileSystem.File.Exists(path))
                {
                    return path;
                }
            }

            // we didn't find the path.
            return null;
        }
    }
}
