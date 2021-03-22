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

using System.ComponentModel.Composition;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.TypeScript.NodeJSLocator.Locators
{
    internal interface IEnvironmentVariableNodeLocator : INodeLocator
    {
    }

    [Export(typeof(IEnvironmentVariableNodeLocator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class EnvironmentVariableNodeLocator : IEnvironmentVariableNodeLocator
    {
        private readonly IFileSystem fileSystem;
        private readonly IEnvironmentSettings environmentSettings;
        private readonly ILogger logger;

        [ImportingConstructor]
        public EnvironmentVariableNodeLocator(ILogger logger)
            : this(new FileSystem(), new EnvironmentSettings(), logger)
        {
        }

        internal EnvironmentVariableNodeLocator(IFileSystem fileSystem, IEnvironmentSettings environmentSettings, ILogger logger)
        {
            this.fileSystem = fileSystem;
            this.environmentSettings = environmentSettings;
            this.logger = logger;
        }

        public string Locate()
        {
            var nodeExePath = environmentSettings.NodeJsExeFilePath();

            if (string.IsNullOrEmpty(nodeExePath))
            {
                logger.WriteLine(Resources.INFO_NoEnvVar);
                return null;
            }

            if (fileSystem.File.Exists(nodeExePath))
            {
                logger.WriteLine(Resources.INFO_EnvVarFileExists, nodeExePath);
                return nodeExePath;
            }

            logger.WriteLine(Resources.ERR_EnvVarFileDoesNotExist, nodeExePath);
            return null;
        }
    }
}
