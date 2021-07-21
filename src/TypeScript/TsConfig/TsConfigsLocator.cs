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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.TypeScript.TsConfig
{
    internal interface ITsConfigsLocator
    {
        /// <summary>
        /// Returns all the tsconfig files in the project directory of the given <see cref="sourceFilePath"/>.
        /// </summary>
        IReadOnlyList<string> Locate(string sourceFilePath);
    }

    [Export(typeof(ITsConfigsLocator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class TsConfigsLocator : ITsConfigsLocator
    {
        private readonly IProjectDirectoryProvider projectDirectoryProvider;
        private readonly IFileSystem fileSystem;

        [ImportingConstructor]
        public TsConfigsLocator(IProjectDirectoryProvider projectDirectoryProvider)
            : this(projectDirectoryProvider, new FileSystem())
        {
        }

        internal TsConfigsLocator(IProjectDirectoryProvider projectDirectoryProvider, IFileSystem fileSystem)
        {
            this.projectDirectoryProvider = projectDirectoryProvider;
            this.fileSystem = fileSystem;
        }

        public IReadOnlyList<string> Locate(string sourceFilePath)
        {
            var projectDirectory = projectDirectoryProvider.GetProjectDirectory(sourceFilePath);

            if (string.IsNullOrEmpty(projectDirectory))
            {
                return Array.Empty<string>();
            }

            // This search behavior matches the observed behavior of VS itself:
            // the tsconfig doesn't have to be included in the project but it needs to be physically under the project's directory
            var foundTsConfigs = fileSystem
                .Directory
                .EnumerateFiles(projectDirectory, "tsconfig.json", SearchOption.AllDirectories)
                .Where(x=> !x.Contains("\\node_modules\\"));

            return foundTsConfigs.ToArray();
        }
    }
}
