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
using System.Diagnostics;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.TypeScript.NodeJSLocator
{
    [Export(typeof(INodeLocator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class NodeLocatorAggregator : INodeLocator
    {
        private readonly ILogger logger;
        private readonly Func<string, Version> getNodeExeVersion;
        private readonly IEnumerable<INodeLocator> nodeLocators;

        [ImportingConstructor]
        public NodeLocatorAggregator(INodeLocatorsProvider nodeLocatorsProvider, ILogger logger)
            : this(nodeLocatorsProvider, logger, GetNodeVersion)
        {
        }

        internal NodeLocatorAggregator(INodeLocatorsProvider nodeLocatorsProvider, ILogger logger, Func<string, Version> getNodeExeVersion)
        {
            this.logger = logger;
            this.getNodeExeVersion = getNodeExeVersion;
            nodeLocators = nodeLocatorsProvider.Get();
        }

        /// <summary>
        /// Returns the absolute file path of a compatible `node.exe`, or null if no compatible version was found.
        /// </summary>
        public string Locate()
        {
            foreach (var nodeLocator in nodeLocators)
            {
                var nodeExePath = nodeLocator.Locate();

                if (!string.IsNullOrEmpty(nodeExePath) && IsCompatibleVersion(nodeExePath))
                {
                    logger.WriteLine(Resources.INFO_FoundCompatibleVersion, nodeExePath);
                    return nodeExePath;
                }
            }

            logger.WriteLine(Resources.ERR_NoCompatibleVersion);
            return null;
        }

        private bool IsCompatibleVersion(string nodeExePath)
        {
            var nodeVersion = getNodeExeVersion(nodeExePath);

            logger.WriteLine(Resources.INFO_NodeVersion, nodeVersion.ToString(), nodeExePath);

            if (nodeVersion.Major < 10)
            {
                logger.WriteLine(Resources.ERR_IncompatibleVersion);
                return false;
            }

            if (nodeVersion.Major == 11)
            {
                logger.WriteLine(Resources.ERR_IncompatibleVersion11);
                return false;
            }

            return true;
        }

        private static Version GetNodeVersion(string path)
        {
            var version = FileVersionInfo.GetVersionInfo(path);

            return new Version(version.ProductMajorPart, version.ProductMinorPart, version.ProductBuildPart);
        }
    }
}
