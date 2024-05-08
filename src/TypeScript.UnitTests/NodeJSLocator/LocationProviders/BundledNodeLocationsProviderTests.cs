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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TypeScript.NodeJSLocator.LocationProviders;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.NodeJSLocator.LocationProviders
{
    [TestClass]
    public class BundledNodeLocationsProviderTests
    {
        [TestMethod]
        public void Get_FailsToRetrieveVsInstallDirectory_EmptyList()
        {
            const string installDir = "c:\\test\\";

            var testSubject = CreateTestSubject(installDir, shellHrResult: VSConstants.E_FAIL);
            var result = testSubject.Get();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void Get_RetrievesVsInstallDirectory_ReturnsFilePaths()
        {
            const string installDir = "c:\\test\\";

            var testSubject = CreateTestSubject(installDir);
            var result = testSubject.Get();

            result.Should().BeEquivalentTo(
                installDir + BundledNodeLocationsProvider.MsBuildPath,
                installDir + BundledNodeLocationsProvider.VsBundledPath);
        }

        private BundledNodeLocationsProvider CreateTestSubject(string installDirectory, int shellHrResult = VSConstants.S_OK)
        {
            object installDir = installDirectory;
            var vsShell = new Mock<IVsShell>();
            vsShell.Setup(x => x.GetProperty((int) __VSSPROPID2.VSSPROPID_InstallRootDir, out installDir)).Returns(shellHrResult);

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsShell))).Returns(vsShell.Object);

            return new BundledNodeLocationsProvider(serviceProvider.Object, Mock.Of<ILogger>());
        }
    }
}
