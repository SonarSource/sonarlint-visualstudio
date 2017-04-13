/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using NuGet.VisualStudio;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class NuGetHelperTests
    {
        #region Tests

        [TestMethod]
        public void NuGetHelper_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => NuGetHelper.TryInstallPackage(null, new ProjectMock("123"), "123"));
            Exceptions.Expect<ArgumentNullException>(() => NuGetHelper.TryInstallPackage(CreateServiceProvider(), null, "123"));
            Exceptions.Expect<ArgumentNullException>(() => NuGetHelper.TryInstallPackage(CreateServiceProvider(), new ProjectMock("123"), null));
        }

        [TestMethod]
        public void NuGetHelper_HandleFailures()
        {
            // Arrange
            ConfigurableServiceProvider sp = CreateServiceProvider();
            var outputWindow = new ConfigurableVsOutputWindow();
            var outputPane = outputWindow.GetOrCreateSonarLintPane();
            sp.RegisterService(typeof(SVsOutputWindow), outputWindow);

            // Case 1: No MEF service
            // Act + Assert
            using (new AssertIgnoreScope()) // Missing MEF service
            {
                NuGetHelper.TryInstallPackage(sp, new ProjectMock("prj"), "pcg").Should().BeFalse("No MEF service should be resulted with a false returned value");
            }
            outputPane.AssertOutputStrings(0);

            // Case 2: Exception from the service
            sp.RegisterService(typeof(SComponentModel), ConfigurableComponentModel.CreateWithExports(MefTestHelpers.CreateExport<IVsPackageInstaller>(new ConfigurablePackageInstaller(simulateInstallerException: true))), replaceExisting: true);
            // Act + Assert
            NuGetHelper.TryInstallPackage(sp, new ProjectMock("prj"), "pcg").Should().BeFalse("Non critical exception should result with a false returned value");
            outputPane.AssertOutputStrings(1);
        }

        [TestMethod]
        public void NuGetHelper_SuccessfulCalls()
        {
            // Arrange
            var package = new PackageName(Guid.NewGuid().ToString("N"), new SemanticVersion("1.0"));
            var availablePackages = new[] { package };

            ConfigurableServiceProvider sp = CreateServiceProvider();
            sp.RegisterService(typeof(SComponentModel), ConfigurableComponentModel.CreateWithExports(MefTestHelpers.CreateExport<IVsPackageInstaller>(new ConfigurablePackageInstaller(availablePackages, simulateInstallerException: false))), replaceExisting: true);

            // Act + Assert
            NuGetHelper.TryInstallPackage(sp, new ProjectMock("prj"), package.Id, package.Version.ToNormalizedString()).Should().BeTrue("The package is expected to be installed successfully");
        }

        #endregion Tests

        #region Helpers

        private static ConfigurableServiceProvider CreateServiceProvider()
        {
            ConfigurableServiceProvider sp = new ConfigurableServiceProvider();
            sp.RegisterService(typeof(SComponentModel), new ConfigurableComponentModel());
            return sp;
        }

        #endregion Helpers
    }
}