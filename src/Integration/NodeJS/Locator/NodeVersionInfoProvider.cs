/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.Core.JsTs;

namespace SonarLint.VisualStudio.Integration.NodeJS.Locator
{
    [Export(typeof(INodeVersionInfoProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class NodeVersionInfoProvider : INodeVersionInfoProvider
    {
        private readonly INodeLocationsProvider nodeLocationsProvider;
        private readonly IFileSystem fileSystem;
        private readonly Func<string, Version> getNodeExeVersion;

        [ImportingConstructor]
        public NodeVersionInfoProvider(INodeLocationsProvider nodeLocationsProvider)
            : this(nodeLocationsProvider, new FileSystem(), GetNodeVersion)
        {
        }

        internal NodeVersionInfoProvider(INodeLocationsProvider nodeLocationsProvider,
            IFileSystem fileSystem,
            Func<string, Version> getNodeExeVersion)
        {
            this.nodeLocationsProvider = nodeLocationsProvider;
            this.fileSystem = fileSystem;
            this.getNodeExeVersion = getNodeExeVersion;
        }

        public IEnumerable<NodeVersionInfo> GetAllNodeVersions()
        {
            foreach (var nodeExePath in nodeLocationsProvider.Get())
            {
                if (string.IsNullOrEmpty(nodeExePath) || !fileSystem.File.Exists(nodeExePath))
                {
                    continue;
                }

                var nodeVersion = getNodeExeVersion(nodeExePath);

                // Perf optimization: we we're using yield to allow the caller to early-out once a suitable node version has been found
                yield return new NodeVersionInfo(nodeExePath, nodeVersion);
            }
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
