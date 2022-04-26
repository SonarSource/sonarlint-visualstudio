/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.Diagnostics;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.TypeScript.NodeJSLocator
{
    internal interface ICompatibleNodeLocator
    {
        /// <summary>
        /// Returns the absolute file path of a compatible `node.exe`, or null if no compatible version was found.
        /// </summary>
        string Locate();
    }

    [Export(typeof(ICompatibleNodeLocator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CompatibleNodeLocator : ICompatibleNodeLocator
    {
        private readonly INodeLocationsProvider nodeLocationsProvider;
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;
        private readonly Func<string, Version> getNodeExeVersion;

        [ImportingConstructor]
        public CompatibleNodeLocator(INodeLocationsProvider nodeLocationsProvider, ILogger logger)
            : this(nodeLocationsProvider, logger, new FileSystem(), GetNodeVersion)
        {
        }

        internal CompatibleNodeLocator(INodeLocationsProvider nodeLocationsProvider,
            ILogger logger,
            IFileSystem fileSystem,
            Func<string, Version> getNodeExeVersion)
        {
            this.nodeLocationsProvider = nodeLocationsProvider;
            this.logger = logger;
            this.fileSystem = fileSystem;
            this.getNodeExeVersion = getNodeExeVersion;
        }

        public string Locate()
        {
            foreach (var nodeExePath in nodeLocationsProvider.Get())
            {
                if (string.IsNullOrEmpty(nodeExePath) || !fileSystem.File.Exists(nodeExePath))
                {
                    continue;
                }

                var nodeVersion = getNodeExeVersion(nodeExePath);

                if (!IsCompatibleVersion(nodeVersion))
                {
                    logger.WriteLine(Resources.ERR_IncompatibleVersion, nodeVersion, nodeExePath);
                    continue;
                }

                logger.WriteLine(Resources.INFO_FoundCompatibleVersion, nodeVersion, nodeExePath);
                return nodeExePath;
            }

            logger.WriteLine(Resources.ERR_NoCompatibleVersion);
            return null;
        }

        internal static bool IsCompatibleVersion(Version nodeVersion)
        {
            return nodeVersion.Major >= 10 && nodeVersion.Major != 11;
        }

        /// <summary>
        /// Based on MS nodejstools https://github.com/microsoft/nodejstools/blob/275e85d5cd95cad9122f59a76f9e49bead66101b/Nodejs/Product/Nodejs/Nodejs.cs#L22
        /// </summary>
        internal static Version GetNodeVersion(string path)
        {
            var version = FileVersionInfo.GetVersionInfo(path);

            return new Version(version.ProductMajorPart, version.ProductMinorPart, version.ProductBuildPart);
        }
    }
}
