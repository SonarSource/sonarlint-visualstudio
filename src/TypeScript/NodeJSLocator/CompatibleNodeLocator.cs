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
using SonarLint.VisualStudio.Core.JsTs;
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
        private readonly INodeVersionInfoProvider nodeVersionInfoProvider;
        private readonly ILogger logger;

        [ImportingConstructor]
        public CompatibleNodeLocator(INodeVersionInfoProvider nodeVersionInfoProvider, ILogger logger)
        {
            this.nodeVersionInfoProvider = nodeVersionInfoProvider;
            this.logger = logger;
        }

        public string Locate()
        {
            foreach (var nodeVersionInfo in nodeVersionInfoProvider.GetAllNodeVersions())
            {
                if (!IsCompatibleVersion(nodeVersionInfo.Version))
                {
                    logger.WriteLine(Resources.ERR_IncompatibleVersion, nodeVersionInfo.Version, nodeVersionInfo.NodeExePath);
                    continue;
                }

                logger.WriteLine(Resources.INFO_FoundCompatibleVersion, nodeVersionInfo.Version, nodeVersionInfo.NodeExePath);
                return nodeVersionInfo.NodeExePath;
            }

            logger.WriteLine(Resources.ERR_NoCompatibleVersion);
            return null;
        }

        internal static bool IsCompatibleVersion(Version nodeVersion)
        {
            return nodeVersion.Major >= 10 && nodeVersion.Major != 11;
        }
    }
}
