/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class VsInfoServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<VsInfoService, IVsInfoService>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(CreateConfiguredServiceProvider("anypath").Object));
        }

        [TestMethod]
        public void Create_VsShellCallSucceeds_ReturnsExpectedPath()
        {
            var serviceProvider = CreateConfiguredServiceProvider("c:\\test\\");

            var testSubject = new VsInfoService(serviceProvider.Object);

            testSubject.InstallRootDir.Should().Be("c:\\test\\");
        }

        [TestMethod]
        public void Create_VsShellCallFails_ExceptionThrown()
        {
            var logger = new TestLogger();
            var serviceProvider = CreateConfiguredServiceProvider("c:\\test\\", shellHrResult: -123);

            Action act = () => new VsInfoService(serviceProvider.Object);
            act.Should().ThrowExactly<COMException>();
        }

        private Mock<IServiceProvider> CreateConfiguredServiceProvider(string installDirectory, int shellHrResult = VSConstants.S_OK)
        {
            object installDir = installDirectory;
            var vsShell = new Mock<IVsShell>();
            vsShell.Setup(x => x.GetProperty((int)__VSSPROPID2.VSSPROPID_InstallRootDir, out installDir)).Returns(shellHrResult);

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsShell))).Returns(vsShell.Object);

            return serviceProvider;
        }
    }
}
