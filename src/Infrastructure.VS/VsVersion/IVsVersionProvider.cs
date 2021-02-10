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
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Setup.Configuration;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Infrastructure.VS.VsVersion
{
    public interface IVsVersionProvider
    {
        /// <summary>
        /// Attempts to retrieve current VS version information.
        /// Logs exceptions and returns null if a failure occurred.
        /// </summary>
        IVsVersion TryGet();
    }

    [Export(typeof(IVsVersionProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class VsVersionProvider : IVsVersionProvider
    {
        private readonly ISetupConfiguration setupConfiguration;
        private readonly ILogger logger;
        private readonly IVsShell vsShell;
        private IVsVersion version;

        [ImportingConstructor]
        public VsVersionProvider([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, ILogger logger)
            : this(serviceProvider, new SetupConfiguration(), logger)
        {
        }

        internal VsVersionProvider(IServiceProvider serviceProvider, ISetupConfiguration setupConfiguration, ILogger logger)
        {
            this.setupConfiguration = setupConfiguration;
            this.logger = logger;
            vsShell = serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
        }

        IVsVersion IVsVersionProvider.TryGet()
        {
            version ??= CalculateVersion();

            return version;
        }

        private IVsVersion CalculateVersion()
        {
            try
            {
                vsShell.GetProperty((int)__VSSPROPID.VSSPROPID_InstallDirectory, out var installDir);

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
                logger.LogDebug(Resources.FailedToCalculateVsVersion, ex);

                return null;
            }
        }
    }
}
