﻿/*
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
using SonarLint.VisualStudio.TypeScript.Notifications;

namespace SonarLint.VisualStudio.TypeScript.NodeJSLocator
{
    [Export(typeof(ICompatibleNodeLocator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CompatibleNodeLocator : ICompatibleNodeLocator
    {
        private readonly INodeVersionInfoProvider nodeVersionInfoProvider;
        private readonly IUnsupportedNodeVersionNotificationService unsupportedNodeNotificationService;
        private readonly ILogger logger;

        [ImportingConstructor]
        public CompatibleNodeLocator(INodeVersionInfoProvider nodeVersionInfoProvider,
            IUnsupportedNodeVersionNotificationService unsupportedNodeNotificationService,
            ILogger logger)
        {
            this.nodeVersionInfoProvider = nodeVersionInfoProvider;
            this.unsupportedNodeNotificationService = unsupportedNodeNotificationService;
            this.logger = logger;
        }

        public NodeVersionInfo Locate()
        {
            foreach (var nodeVersionInfo in nodeVersionInfoProvider.GetAllNodeVersions())
            {
                if (!IsCompatibleVersion(nodeVersionInfo.Version))
                {
                    logger.WriteLine(Resources.ERR_IncompatibleVersion, nodeVersionInfo.Version, nodeVersionInfo.NodeExePath);
                    continue;
                }

                logger.WriteLine(Resources.INFO_FoundCompatibleVersion, nodeVersionInfo.Version, nodeVersionInfo.NodeExePath);
                return nodeVersionInfo;
            }

            logger.WriteLine(Resources.ERR_NoCompatibleVersion);
            unsupportedNodeNotificationService.Show();
            return null;
        }

        internal static bool IsCompatibleVersion(Version nodeVersion)
        {
            // Minimum supported version 14.17.0
            return (nodeVersion.Major == 14 && nodeVersion.Minor >= 17) || nodeVersion.Major > 14;
        }
    }
}
