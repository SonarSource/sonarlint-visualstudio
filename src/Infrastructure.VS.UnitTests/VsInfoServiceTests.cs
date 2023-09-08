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
            => MefTestHelpers.CheckTypeCanBeImported<VsInfoService, IVsInfoService>(
                MefTestHelpers.CreateExport<IVsUIServiceOperation>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
            => MefTestHelpers.CheckIsSingletonMefComponent<VsInfoService>();

        [TestMethod]
        public void MefCtor_DoesNotCallAnyServices()
        {
            var serviceOp = new Mock<IVsUIServiceOperation>();

            _ = new VsInfoService(serviceOp.Object);

            // The MEF constructor should be free-threaded, which it will be if
            // it doesn't make any external calls.
            serviceOp.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Create_VsShellCallSucceeds_ReturnsExpectedPath()
        {
            var serviceOp = CreateConfiguredServiceOperation("c:\\test\\");

            var testSubject = new VsInfoService(serviceOp);

            testSubject.InstallRootDir.Should().Be("c:\\test\\");
        }

        [TestMethod]
        public void Create_VsShellCallFails_ExceptionThrown()
        {
            var logger = new TestLogger();
            var serviceOp = CreateConfiguredServiceOperation("c:\\test\\", shellHrResult: -123);

            var testSubject = new VsInfoService(serviceOp);

            Action act = () => _ = testSubject.InstallRootDir;
            act.Should().ThrowExactly<COMException>();
        }

        private IVsUIServiceOperation CreateConfiguredServiceOperation(string installDirectory, int shellHrResult = VSConstants.S_OK)
        {
            object installDir = installDirectory;
            var vsShell = new Mock<IVsShell>();
            vsShell.Setup(x => x.GetProperty((int)__VSSPROPID2.VSSPROPID_InstallRootDir, out installDir)).Returns(shellHrResult);

            var serviceOp = CreateServiceOperation(vsShell.Object);

            return serviceOp;
        }

        private IVsUIServiceOperation CreateServiceOperation(IVsShell svcToPassToCallback)
        {
            var serviceOp = new Mock<IVsUIServiceOperation>();

            // Set up the mock to invoke the operation with the supplied VS service
            serviceOp.Setup(x => x.Execute<SVsShell, IVsShell, string>(It.IsAny<Func<IVsShell, string>>()))
                .Returns<Func<IVsShell, string>>(op => op(svcToPassToCallback));

            return serviceOp.Object;
        }
    }
}
