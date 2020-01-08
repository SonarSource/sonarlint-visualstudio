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
using SonarLint.VisualStudio.Integration.Vsix;
using System.IO;
using VSIX = SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintDaemon
{
    [TestClass]
    public class DaemonInstallerTests
    {
        private string tempPath;
        private string storagePath;
        private TestLogger logger;
        private DaemonInstaller installer;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void SetUp()
        {
            logger = new TestLogger(logToConsole: true);

            tempPath = Path.Combine(TestContext.DeploymentDirectory, Path.GetRandomFileName());
            storagePath = Path.Combine(TestContext.DeploymentDirectory, Path.GetRandomFileName());
            Directory.CreateDirectory(tempPath);
            Directory.CreateDirectory(storagePath);
            installer = new DaemonInstaller(logger, storagePath, tempPath);

            logger.Reset(); // clear any messages logged during construction
        }

        [TestMethod]
        public void DownloadUrlInEnvironmentVar_NotSet_DefaultUsed()
        {
            using (var scope = new EnvironmentVariableScope())
            {
                // Arrange - make sure the variable is not set
                scope.SetVariable(VSIX.DaemonInstaller.SonarLintDownloadUrlEnvVar, "");

                // Act
                var installer = new DaemonInstaller(logger, "c:\\storage", "d:\\temp");

                // Assert
                installer.DownloadUrl.Should().Be(VSIX.DaemonInstaller.DefaultDownloadUrl);
                installer.DaemonVersion.Should().Be(VSIX.DaemonInstaller.DefaultDaemonVersion);
                installer.InstallationPath.Should().Be($"c:\\storage\\sonarlint-daemon-{VSIX.DaemonInstaller.DefaultDaemonVersion}-windows");
                installer.ZipFilePath.Should().Be($"d:\\temp\\sonarlint-daemon-{VSIX.DaemonInstaller.DefaultDaemonVersion}-windows.zip");
            }
        }

        [TestMethod]
        public void DownloadUrlInEnvironmentVar_InvalidUrl_UseDefault()
        {
            using (var scope = new EnvironmentVariableScope())
            {
                // Arrange
                scope.SetVariable(VSIX.DaemonInstaller.SonarLintDownloadUrlEnvVar, "invalid uri");

                // Act
                var installer = new VSIX.DaemonInstaller(logger);

                // Assert
                installer.DownloadUrl.Should().Be(VSIX.DaemonInstaller.DefaultDownloadUrl);
                installer.DaemonVersion.Should().Be(VSIX.DaemonInstaller.DefaultDaemonVersion);
            }
        }

        [TestMethod]
        public void DownloadUrlInEnvironmentVar_InvalidVersion_UseDefault()
        {
            using (var scope = new EnvironmentVariableScope())
            {
                // Arrange
                scope.SetVariable(VSIX.DaemonInstaller.SonarLintDownloadUrlEnvVar, "http://somewhere/sonarlint-daemon.zip");

                // Act
                var installer = new VSIX.DaemonInstaller(logger);

                // Assert
                installer.DownloadUrl.Should().Be(VSIX.DaemonInstaller.DefaultDownloadUrl);
                installer.DaemonVersion.Should().Be(VSIX.DaemonInstaller.DefaultDaemonVersion);
            }
        }

        [TestMethod]
        public void DownloadUrlInEnvironmentVar_Valid_UseSupplied()
        {
            using (var scope = new EnvironmentVariableScope())
            {
                // Arrange
                scope.SetVariable(VSIX.DaemonInstaller.SonarLintDownloadUrlEnvVar,
                    "https://repox.jfrog.io/repox/sonarsource/org/sonarsource/sonarlint/core/sonarlint-daemon/4.3.0.2450/sonarlint-daemon-4.3.0.2450-windows.zip");

                // Act
                var installer = new DaemonInstaller(logger, "c:\\storagePath\\", "d:\\tempPath\\");

                // Assert
                installer.DownloadUrl.Should().Be("https://repox.jfrog.io/repox/sonarsource/org/sonarsource/sonarlint/core/sonarlint-daemon/4.3.0.2450/sonarlint-daemon-4.3.0.2450-windows.zip");
                installer.DaemonVersion.Should().Be("4.3.0.2450");
                installer.InstallationPath.Should().Be("c:\\storagePath\\sonarlint-daemon-4.3.0.2450-windows");
                installer.ZipFilePath.Should().Be("d:\\tempPath\\sonarlint-daemon-4.3.0.2450-windows.zip");
            }
        }

        [TestMethod]
        public void IsInstalled()
        {
            // Sanity check - not expecting the exe directory to exist to start with
            var installationBaseDir = Path.Combine(TestContext.DeploymentDirectory, TestContext.TestName);
            Directory.Exists(installationBaseDir).Should().BeFalse();

            installer = new DaemonInstaller(logger, installationBaseDir, "c:\\dummy");

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
    }
}
