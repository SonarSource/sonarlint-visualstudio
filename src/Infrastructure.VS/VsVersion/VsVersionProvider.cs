/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using Microsoft.VisualStudio.Setup.Configuration;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.VsVersion;

namespace SonarLint.VisualStudio.Infrastructure.VS.VsVersion
{
    [Export(typeof(IVsVersionProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class VsVersionProvider : IVsVersionProvider
    {
        public IVsVersion Version { get; }

        [ImportingConstructor]
        public VsVersionProvider([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, ILogger logger)
            : this(serviceProvider, new SetupConfigurationProvider(), logger)
        {
        }

        internal VsVersionProvider(IServiceProvider serviceProvider, ISetupConfigurationProvider setupConfigurationProvider, ILogger logger)
        {
            Version = CalculateVersion(serviceProvider, setupConfigurationProvider, logger);
        }

        private static IVsVersion CalculateVersion(IServiceProvider serviceProvider, ISetupConfigurationProvider setupConfigurationProvider, ILogger logger)
        {
            try
            {
                var vsShell = serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
                vsShell.GetProperty((int)__VSSPROPID.VSSPROPID_InstallDirectory, out var installDir);

                var setupConfiguration = setupConfigurationProvider.Get();
                var setupInstance = setupConfiguration.GetInstanceForPath((string)installDir);
                var productName = setupInstance.GetDisplayName();
                var productVersion = setupInstance.GetInstallationVersion();
                var catalog = (ISetupInstanceCatalog)setupInstance;
                var catalogInfo = catalog.GetCatalogInfo();
                var productDisplayVersion = catalogInfo.GetValue("productDisplayVersion") as string;

                logger.WriteLine(Resources.VsVersionDetails,
                    productName,
                    productVersion,
                    productDisplayVersion);

                return new VsVersion(productName, productVersion, productDisplayVersion);
            }
            catch (Exception ex)
            {
                logger.LogVerbose(Resources.FailedToCalculateVsVersion, ex);

                return null;
            }
        }
    }
}
