/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

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

                var message = string.Format(Strings.FailedDuringNuGetPackageInstall, packageId, project.Name, ex.Message);
                VsShellUtils.WriteToSonarLintOutputPane(serviceProvider, Strings.SubTextPaddingFormat, message);
                return false;
            }
        }
    }
}
