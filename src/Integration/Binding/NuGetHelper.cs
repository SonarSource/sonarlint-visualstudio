//-----------------------------------------------------------------------
// <copyright file="NuGetHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using NuGet.VisualStudio;
using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.Diagnostics;
using System.Linq;

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

                VsShellUtils.WriteToGeneralOutputPane(serviceProvider, Strings.FailedDuringNuGetPackageInstall, packageId, project.Name, ex.ToString());
                return false;
            }
        }
    }
}
