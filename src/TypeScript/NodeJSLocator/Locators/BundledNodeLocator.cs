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
using System.IO;
using System.IO.Abstractions;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.TypeScript.NodeJSLocator.Locators
{
    internal class BundledNodeLocator : INodeLocator
    {
        internal const string MsBuildPath = "MSBuild\\Microsoft\\VisualStudio\\NodeJs\\node.exe";
        internal const string VsBundledPath = "Common7\\ServiceHub\\Hosts\\ServiceHub.Host.Node.x86\\ServiceHub.Host.Node.x86.exe";

        private readonly IFileSystem fileSystem;
        private readonly ILogger logger;
        private readonly string msbuildNodeExePath;
        private readonly string bundledNodeExePath;

        public BundledNodeLocator(IServiceProvider serviceProvider, ILogger logger)
            : this(serviceProvider, new FileSystem(), logger)
        {
        }

        internal BundledNodeLocator(IServiceProvider serviceProvider, IFileSystem fileSystem, ILogger logger)
        {
            this.fileSystem = fileSystem;
            this.logger = logger;
            var vsShell = serviceProvider.GetService(typeof(SVsShell)) as IVsShell;

            // e.g. C:\Program Files (x86)\Microsoft Visual Studio\2019\Preview\
            vsShell.GetProperty((int)__VSSPROPID2.VSSPROPID_InstallRootDir, out var installDir);

            msbuildNodeExePath = Path.Combine((string)installDir, MsBuildPath);
            bundledNodeExePath = Path.Combine((string)installDir, VsBundledPath);
        }

        public string Locate()
        {
            var filePath = fileSystem.File.Exists(msbuildNodeExePath)
                ? msbuildNodeExePath
                : fileSystem.File.Exists(bundledNodeExePath)
                    ? bundledNodeExePath
                    : null;

            if (string.IsNullOrEmpty(filePath))
            {
                logger.WriteLine(Resources.INFO_NoBundledNode);
                return null;
            }

            logger.WriteLine(Resources.INFO_FoundBundledNode, bundledNodeExePath);
            return filePath;
        }
    }
}
