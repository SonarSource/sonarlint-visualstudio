﻿/*
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

using Microsoft.VisualStudio.Setup.Configuration;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.VsVersion;
using SonarLint.VisualStudio.Infrastructure.VS.VsVersion;
using IVsShell = Microsoft.VisualStudio.Shell.Interop.IVsShell;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class VsVersionProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
            => MefTestHelpers.CheckTypeCanBeImported<VsVersionProvider, IVsVersionProvider>(
                MefTestHelpers.CreateExport<IVsUIServiceOperation>(),
                MefTestHelpers.CreateExport<ILogger>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
            => MefTestHelpers.CheckIsSingletonMefComponent<VsVersionProvider>();

        [TestMethod]
        public void MefCtor_DoesNotCallAnyServices()
        {
            var serviceOp = new Mock<IVsUIServiceOperation>();
            var logger = new Mock<ILogger>();

            _ = new VsVersionProvider(serviceOp.Object, logger.Object);

            // The MEF constructor should be free-threaded, which it will be if
            // it doesn't make any external calls.
            serviceOp.Invocations.Should().BeEmpty();
            logger.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Version_CalculatesVersion()
        {
            const string name = "Microsoft Visual Studio Enterprise 2019";
            const string installationVersion = "16.9.30914.41";
            const string displayVersion = "16.9.0 Preview 3.0";
            const string installDirectory = "some directory";

            var vsShell = CreateVsShell(installDirectory);
            var setupConfig = CreateSetupConfiguration(installDirectory, name, installationVersion, displayVersion);

            var testSubject = CreateTestSubject(vsShell.Object, setupConfig.Object);
            var vsVersion = testSubject.Version;

            vsVersion.Should().NotBeNull();
            vsVersion.DisplayName.Should().Be(name);
            vsVersion.InstallationVersion.Should().Be(installationVersion);
            vsVersion.DisplayVersion.Should().Be(displayVersion);
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
            var vsShell = CreateVsShell(errorToReturn: new Exception("this is a test"));

            var testSubject = CreateTestSubject(vsShell.Object, null);
            var vsVersion = testSubject.Version;

            vsVersion.Should().BeNull();

            vsShell.VerifyAll();
        }

        [TestMethod]
        [Description("Regression test for https://github.com/SonarSource/sonarlint-visualstudio/issues/2229")]
        public void Version_FailureToRetrieveSetupConfiguration_Null()
        {
            const string installDirectory = "some directory";
            var vsShell = CreateVsShell(installDirectory);

            var setupConfigurationProvider = new Mock<ISetupConfigurationProvider>();
            setupConfigurationProvider
                .Setup(x => x.Get())
                .Throws(new Exception("this is a test"));

            var testSubject = new VsVersionProvider(CreateServiceOperation(vsShell.Object), setupConfigurationProvider.Object, Mock.Of<ILogger>());;

            testSubject.Version.Should().BeNull();

            setupConfigurationProvider.VerifyAll();
        }

        private IVsVersionProvider CreateTestSubject(IVsShell vsShell, ISetupConfiguration2 setupConfiguration, ILogger logger = null)
        {
            logger ??= Mock.Of<ILogger>();

            var setupConfigurationProvider = new Mock<ISetupConfigurationProvider>();
            setupConfigurationProvider.Setup(x => x.Get()).Returns(setupConfiguration);

            return new VsVersionProvider(CreateServiceOperation(vsShell), setupConfigurationProvider.Object, logger);
        }

        private IVsUIServiceOperation CreateServiceOperation(IVsShell svcToPassToCallback)
        {
            var serviceOp = new Mock<IVsUIServiceOperation>();

            // Set up the mock to invoke the operation with the supplied VS service
            serviceOp.Setup(x => x.Execute<SVsShell, IVsShell, IVsVersion>(It.IsAny<Func<IVsShell, IVsVersion>>()))
                .Returns<Func<IVsShell, IVsVersion>>(op => op(svcToPassToCallback));

            return serviceOp.Object;
        }

        private static Mock<IVsShell> CreateVsShell(string installDirectory = null, Exception errorToReturn = null)
        {
            object installDir = installDirectory;
            var vsShell = new Mock<IVsShell>();
            var setup = vsShell.Setup(x => x.GetProperty((int)__VSSPROPID.VSSPROPID_InstallDirectory, out installDir));

            if (errorToReturn != null)
            {
                setup.Throws(errorToReturn);
            }

            return vsShell;
        }

        private static Mock<ISetupConfiguration2> CreateSetupConfiguration(string installDirectory, string name = "some name", string buildVersion = "build version", string displayVersion = "display version")
        {
            var catalogInfo = new Mock<ISetupPropertyStore>();
            catalogInfo.Setup(x => x.GetValue("productDisplayVersion")).Returns(displayVersion);

            var setupInstance = new Mock<ISetupInstance>();
            setupInstance.Setup(x => x.GetDisplayName(0)).Returns(name);
            setupInstance.Setup(x => x.GetInstallationVersion()).Returns(buildVersion);
            setupInstance.As<ISetupInstanceCatalog>().Setup(x => x.GetCatalogInfo()).Returns(catalogInfo.Object);

            var setupConfig = new Mock<ISetupConfiguration2>();
            setupConfig.Setup(x => x.GetInstanceForPath(installDirectory)).Returns(setupInstance.Object);

            return setupConfig;
        }
    }
}
