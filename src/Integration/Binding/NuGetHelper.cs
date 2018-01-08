/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using NuGet.VisualStudio;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal static class NuGetHelper
    {
        public static IVsPackageInstaller LoadService(IServiceProvider serviceProvider)
        {
            IComponentModel componentModel = serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            var installer = componentModel.GetExtensions<IVsPackageInstaller>().SingleOrDefault();
            Debug.Assert(installer != null, "Cannot find IVsPackageInstaller");
            return installer;
        }

        public static bool TryInstallPackage(IServiceProvider serviceProvider, Project project, string packageId, string version = null)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
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
                VsShellUtils.WriteToSonarLintOutputPane(serviceProvider, Strings.SubTextPaddingFormat, message);
                return false;
            }
        }
    }
}
