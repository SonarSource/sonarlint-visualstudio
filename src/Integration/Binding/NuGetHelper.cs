/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using EnvDTE;
using Microsoft.VisualStudio;
using NuGet.VisualStudio;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal static class NuGetHelper
    {
        public static IVsPackageInstaller LoadService(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetMefService<IVsPackageInstaller>();
        }

        public static bool TryInstallPackage(IServiceProvider serviceProvider, ILogger logger, Project project, string packageId, string version = null)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentNullException(nameof(packageId));
            }

            IVsPackageInstaller installer = LoadService(serviceProvider);
            if (installer == null)
            {
                return false;
            }

            try
            {
                // The installer will no-op in case the package is installed
                installer.InstallPackage(source: null,
                    project: project,
                    packageId: packageId,
                    version: version,
                    ignoreDependencies: false);
                return true;
            }
            catch (Exception ex)
            {
                if (ErrorHandler.IsCriticalException(ex))
                {
                    throw;
                }

                var message = string.Format(Strings.FailedDuringNuGetPackageInstall, packageId, project.Name, ex.Message);
                logger.WriteLine(Strings.SubTextPaddingFormat, message);
                return false;
            }
        }
    }
}
