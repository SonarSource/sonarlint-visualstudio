/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix;
using System.IO;
using VSIX = SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintDaemon
{
    [TestClass]
    public class DaemonInstallerTests
    {
        private TestLogger logger;
        private Mock<IEnvironmentSettings> envSettingsMock = new Mock<IEnvironmentSettings>();

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void SetUp()
        {
            logger = new TestLogger(logToConsole: true);
            envSettingsMock = new Mock<IEnvironmentSettings>();
        }

        [TestMethod]
        public void DownloadUrlInEnvironmentVar_NotSet_DefaultUsed()
        {
            // Arrange - make sure the variable is not set
            SetUrlEnvironmentVariable(null);

            // Act
            var installer = new DaemonInstaller(logger, "c:\\storage", "d:\\temp", envSettingsMock.Object);

            // Assert
            installer.DownloadUrl.Should().Be(VSIX.DaemonInstaller.DefaultDownloadUrl);
            installer.DaemonVersion.Should().Be(VSIX.DaemonInstaller.DefaultDaemonVersion);
            installer.InstallationPath.Should().Be($"c:\\storage\\sonarlint-daemon-{VSIX.DaemonInstaller.DefaultDaemonVersion}-windows");
            installer.ZipFilePath.Should().Be($"d:\\temp\\sonarlint-daemon-{VSIX.DaemonInstaller.DefaultDaemonVersion}-windows.zip");
        }

        [TestMethod]
        public void DownloadUrlInEnvironmentVar_InvalidUrl_UseDefault()
        {
            // Arrange
            SetUrlEnvironmentVariable("invalid uri");

            // Act
            var installer = new VSIX.DaemonInstaller(logger, "any", "any", envSettingsMock.Object);

            // Assert
            installer.DownloadUrl.Should().Be(VSIX.DaemonInstaller.DefaultDownloadUrl);
            installer.DaemonVersion.Should().Be(VSIX.DaemonInstaller.DefaultDaemonVersion);
        }

        [TestMethod]
        public void DownloadUrlInEnvironmentVar_InvalidVersion_UseDefault()
        {
            // Arrange
            SetUrlEnvironmentVariable("http://somewhere/sonarlint-daemon.zip");

            // Act
            var installer = new VSIX.DaemonInstaller(logger, "any", "any", envSettingsMock.Object);

            // Assert
            installer.DownloadUrl.Should().Be(VSIX.DaemonInstaller.DefaultDownloadUrl);
            installer.DaemonVersion.Should().Be(VSIX.DaemonInstaller.DefaultDaemonVersion);
        }

        [TestMethod]
        public void DownloadUrlInEnvironmentVar_Valid_UseSupplied()
        {
            // Arrange
            SetUrlEnvironmentVariable(
                "https://repox.jfrog.io/repox/sonarsource/org/sonarsource/sonarlint/core/sonarlint-daemon/4.3.0.2450/sonarlint-daemon-4.3.0.2450-windows.zip");

            // Act
            var installer = new DaemonInstaller(logger, "c:\\storagePath\\", "d:\\tempPath\\", envSettingsMock.Object);

            // Assert
            installer.DownloadUrl.Should().Be("https://repox.jfrog.io/repox/sonarsource/org/sonarsource/sonarlint/core/sonarlint-daemon/4.3.0.2450/sonarlint-daemon-4.3.0.2450-windows.zip");
            installer.DaemonVersion.Should().Be("4.3.0.2450");
            installer.InstallationPath.Should().Be("c:\\storagePath\\sonarlint-daemon-4.3.0.2450-windows");
            installer.ZipFilePath.Should().Be("d:\\tempPath\\sonarlint-daemon-4.3.0.2450-windows.zip");
        }

        [TestMethod]
        public void IsInstalled()
        {
            // Sanity check - not expecting the exe directory to exist to start with
            var installationBaseDir = Path.Combine(TestContext.DeploymentDirectory, TestContext.TestName);
            Directory.Exists(installationBaseDir).Should().BeFalse();

            var installer = new DaemonInstaller(logger, installationBaseDir, "c:\\dummy", envSettingsMock.Object);

            // 1. No directory or file -> false
            installer.IsInstalled().Should().BeFalse();

            // 2. Directory exists but not file
            Directory.CreateDirectory(installer.InstallationPath);
            installer.IsInstalled().Should().BeFalse();

            // 3. Directory and file exist -> true
            var subDir = Path.Combine(installer.InstallationPath, "sub1", "sub2");
            Directory.CreateDirectory(subDir);
            var javaExePath = Path.Combine(subDir, "java.exe");
            File.WriteAllText(javaExePath, "junk");
            installer.IsInstalled().Should().BeTrue();
        }

        private void SetUrlEnvironmentVariable(string val)
        {
            envSettingsMock.Setup(x => x.SonarLintDaemonDownloadUrl()).Returns(val);
        }
    }
}
