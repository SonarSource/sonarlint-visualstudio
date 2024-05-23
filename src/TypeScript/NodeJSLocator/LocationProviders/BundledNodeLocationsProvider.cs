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

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;

using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.TypeScript.NodeJSLocator.LocationProviders
{
    internal class BundledNodeLocationsProvider : INodeLocationsProvider
    {
        internal const string MsBuildPath = "MSBuild\\Microsoft\\VisualStudio\\NodeJs\\node.exe";
        internal const string VsBundledPath = "Common7\\ServiceHub\\Hosts\\ServiceHub.Host.Node.x86\\ServiceHub.Host.Node.x86.exe";

        private readonly IVsShell vsShell;
        private readonly ILogger logger;

        public BundledNodeLocationsProvider(IServiceProvider serviceProvider, ILogger logger)
        {
            this.logger = logger;
            vsShell = serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
        }

        public IReadOnlyCollection<string> Get()
        {
            // e.g. C:\Program Files (x86)\Microsoft Visual Studio\2022\Preview\
            var hr = vsShell.GetProperty((int)__VSSPROPID2.VSSPROPID_InstallRootDir, out var installDir);

            if (ErrorHandler.Failed(hr))
            {
                logger.WriteLine(Resources.ERR_FailedToGetVsInstallDirectory, hr);
                return Array.Empty<string>();
            }

            var msbuildNodeExePath = Path.Combine((string)installDir, MsBuildPath);
            var bundledNodeExePath = Path.Combine((string)installDir, VsBundledPath);

            return new[] { msbuildNodeExePath, bundledNodeExePath };
        }
    }
}
