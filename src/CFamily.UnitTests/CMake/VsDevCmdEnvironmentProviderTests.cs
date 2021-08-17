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
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.CFamily.SystemAbstractions;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.CFamily.UnitTests.CMake
{
    [TestClass]
    public class _VsDevCmdEnvironmentProviderTests
    {
        public TestContext TestContext { get; set; }

        private readonly IVsInfoService ValidVsInfoService = CreateVsInfoService("c:\\any");

        [TestMethod]
        public void Get_MissingFile_ReturnsEmptySettings()
        {

        }

        [TestMethod]
        public void Get_FileExists_ReturnsExpectedSettings()
        {
        }

        [TestMethod]
        public void Get_ExpectedProcessArgumentsPassed()
        {
            var infoService = CreateVsInfoService("c:\\any");

            ProcessStartInfo actualProcessStartInfo = null;

            var process = new Mock<IProcess>();
            var processFactory = new Mock<IProcessFactory>();
            processFactory.Setup(x => x.Start(It.IsAny<ProcessStartInfo>()))
                .Callback<ProcessStartInfo>(x => actualProcessStartInfo = x)
                .Returns(process.Object);

            var testSubject = new VsDevCmdEnvironmentProvider(infoService, new TestLogger(), processFactory.Object);

            // Act
            testSubject.Get("input-script-arguments");

            actualProcessStartInfo.Should().NotBeNull();
            actualProcessStartInfo.FileName.Should().Be(Environment.GetEnvironmentVariable("COMSPEC"));

            actualProcessStartInfo.Arguments.Should().NotBeNull();

            // Split and clean up the command string to make the individual arguments easier to check
            var args = actualProcessStartInfo.Arguments.Split(new string[] { " && " }, StringSplitOptions.None)
                .Select(x => x.Trim())
                .ToArray();

            args[0].Should().Be("/U /K set VSCMD_SKIP_SENDTELEMETRY=1"); // args to cmd.exe and first command in the sequence
            args[1].Should().Be(@"""c:\any\Common7\Tools\VsDevCmd.bat"" input-script-arguments"); // calculated full path to VsDevCmd.bat
            args[2].Should().StartWith("echo SONARLINT_BEGIN_CAPTURE");
            args[3].Should().Be("set");
            args[4].Should().StartWith("echo SONARLINT_END_CAPTURE");
        }

        [TestMethod]
        public void Get_Lifecycle_NoOutput_RunsToCompletion()
        {
            var process = new Mock<IProcess>();
            var processFactory = new Mock<IProcessFactory>();
            processFactory.Setup(x => x.Start(It.IsAny<ProcessStartInfo>()))
                .Returns(process.Object);

            var testSubject = new VsDevCmdEnvironmentProvider(ValidVsInfoService, new TestLogger(), processFactory.Object);

            // Act
            testSubject.Get("any");

            // Just checking the invociation order
            var invokedMembers = process.Invocations.Select(x => x.Method.Name).ToArray();
            invokedMembers.Should().ContainInOrder(
                // Initialize and run
                "set_HandleOutputDataReceived",
                "BeginOutputReadLine",
                "WaitForExit",

                // Cleanup
                "get_HasExited",
                "Kill",
                "Dispose"
                );
        }


        [TestMethod]
        public void Get_DataProcessing_DataBeforeBeginMarkerIsIgnored()
        {
            var process = new Mock<IProcess>();
        }

        [TestMethod]
        public void Get_DataProcessing_ProcessIsKilledWhenEndMarkerReceived()
        {

        }

        [TestMethod]
        public void Get_TimeoutOccurs_ProcessIsKilledAndNullReturned()
        {

        }

        private static IVsInfoService CreateVsInfoService(string installRootDir)
        {
            var infoService = new Mock<IVsInfoService>();
            infoService.SetupGet(x => x.InstallRootDir).Returns(installRootDir);
            return infoService.Object;
        }
    }
}
