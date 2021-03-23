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
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TypeScript.NodeJSLocator.Locators;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.NodeJSLocator.Locators
{
    [TestClass]
    public class BundledNodeLocatorTests
    {
        [TestMethod]
        public void Locate_FailsToRetrieveInstallDirectory_Exception()
        {
            const string installDir = "c:\\test\\";

            var testSubject = CreateTestSubject(installDir, shellHrResult: VSConstants.E_FAIL);
            Action act = () => testSubject.Locate();

            act.Should().Throw<COMException>();
        }

        [TestMethod]
        public void Locate_FileFoundInMsBuild_FilePath()
        {
            const string installDir = "c:\\test\\";

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(installDir + BundledNodeLocator.MsBuildPath)).Returns(true);

            var testSubject = CreateTestSubject(installDir, fileSystem.Object);
            var result = testSubject.Locate();

            result.Should().Be(installDir + BundledNodeLocator.MsBuildPath);

            fileSystem.VerifyAll();
            fileSystem.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Locate_FileNotFoundInMsBuild_FileFoundInBundledFolder_FilePath()
        {
            const string installDir = "c:\\test\\";

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(installDir + BundledNodeLocator.MsBuildPath)).Returns(false);
            fileSystem.Setup(x => x.File.Exists(installDir + BundledNodeLocator.VsBundledPath)).Returns(true);

            var testSubject = CreateTestSubject(installDir, fileSystem.Object);
            var result = testSubject.Locate();

            result.Should().Be(installDir + BundledNodeLocator.VsBundledPath);

            fileSystem.VerifyAll();
            fileSystem.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Locate_FileNotFound_Null()
        {
            const string installDir = "c:\\test\\";

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(It.IsAny<string>())).Returns(false);

            var testSubject = CreateTestSubject(installDir, fileSystem.Object);
            var result = testSubject.Locate();

            result.Should().BeNull();
        }

        private BundledNodeLocator CreateTestSubject(string installDirectory, IFileSystem fileSystem = null, int shellHrResult = VSConstants.S_OK)
        {
            object installDir = installDirectory;
            var vsShell = new Mock<IVsShell>();
            vsShell.Setup(x => x.GetProperty((int) __VSSPROPID2.VSSPROPID_InstallRootDir, out installDir)).Returns(shellHrResult);

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsShell))).Returns(vsShell.Object);

            return new BundledNodeLocator(serviceProvider.Object, fileSystem, Mock.Of<ILogger>());
        }
    }
}
