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
using FluentAssertions;
using Microsoft.VisualStudio.Setup.Configuration;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.VsVersion;
using SonarLint.VisualStudio.Infrastructure.VS.VsVersion;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using IVsShell = Microsoft.VisualStudio.Shell.Interop.IVsShell;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class VsVersionProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<VsVersionProvider, IVsVersionProvider>(null, new[]
            {
                MefTestHelpers.CreateExport<SVsServiceProvider>(CreateServiceProvider(Mock.Of<IVsShell>())),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>())
            });
        }

        [TestMethod]
        public void Version_CalculatesVersion()
        {
            const string name = "Microsoft Visual Studio Enterprise 2019";
            const string buildVersion = "16.9.30914.41";
            const string displayVersion = "16.9.0 Preview 3.0";
            const string installDirectory = "some directory";

            var vsShell = CreateVsShell(installDirectory);
            var setupConfig = CreateSetupConfiguration(installDirectory, name, buildVersion, displayVersion);

            var testSubject = CreateTestSubject(vsShell.Object, setupConfig.Object);
            var vsVersion = testSubject.Version;

            vsVersion.Should().NotBeNull();
            vsVersion.ProductName.Should().Be(name);
            vsVersion.ProductVersion.Should().Be(buildVersion);
            vsVersion.ProductDisplayVersion.Should().Be(displayVersion);
        }

        [TestMethod]
        public void Version_VersionIsCached()
        {
            const string installDirectory = "some directory";

            var vsShell = CreateVsShell(installDirectory);
            var setupConfig = CreateSetupConfiguration(installDirectory);

            var testSubject = CreateTestSubject(vsShell.Object, setupConfig.Object);

            var vsVersion1 = testSubject.Version;

            vsShell.Invocations.Clear();
            setupConfig.Invocations.Clear();

            var vsVersion2 = testSubject.Version;

            vsVersion1.Should().BeSameAs(vsVersion2);

            vsShell.Invocations.Should().BeEmpty();
            setupConfig.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Version_FailureToGetVersion_Null()
        {
            var testSubject = CreateTestSubject(null, null);
            var vsVersion = testSubject.Version;

            vsVersion.Should().BeNull();
        }

        private IVsVersionProvider CreateTestSubject(IVsShell vsShell, ISetupConfiguration setupConfiguration, ILogger logger = null)
        {
            logger ??= Mock.Of<ILogger>();

            return new VsVersionProvider(CreateServiceProvider(vsShell), setupConfiguration, logger);
        }

        private IServiceProvider CreateServiceProvider(IVsShell vsShell)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsShell))).Returns(vsShell);

            return serviceProvider.Object;
        }

        private static Mock<IVsShell> CreateVsShell(string installDirectory)
        {
            object installDir = installDirectory;
            var vsShell = new Mock<IVsShell>();
            vsShell.Setup(x => x.GetProperty((int)__VSSPROPID.VSSPROPID_InstallDirectory, out installDir));

            return vsShell;
        }

        private static Mock<ISetupConfiguration> CreateSetupConfiguration(string installDirectory, string name = "some name", string buildVersion = "build version", string displayVersion = "display version")
        {
            var catalogInfo = new Mock<ISetupPropertyStore>();
            catalogInfo.Setup(x => x.GetValue("productDisplayVersion")).Returns(displayVersion);

            var setupInstance = new Mock<ISetupInstance>();
            setupInstance.Setup(x => x.GetDisplayName(0)).Returns(name);
            setupInstance.Setup(x => x.GetInstallationVersion()).Returns(buildVersion);
            setupInstance.As<ISetupInstanceCatalog>().Setup(x => x.GetCatalogInfo()).Returns(catalogInfo.Object);

            var setupConfig = new Mock<ISetupConfiguration>();
            setupConfig.Setup(x => x.GetInstanceForPath(installDirectory)).Returns(setupInstance.Object);

            return setupConfig;
        }
    }
}
