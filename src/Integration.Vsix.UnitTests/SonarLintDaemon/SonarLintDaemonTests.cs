/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.IO;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VSIX = SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SonarLintDaemonTests
    {
        private string tempPath;
        private string storagePath;
        private VSIX.SonarLintDaemon daemon;
        private TestLogger logger;

        [TestInitialize]
        public void SetUp()
        {
            ISonarLintSettings settings = new Mock<ISonarLintSettings>().Object;
            logger = new TestLogger();

            tempPath = Path.Combine(Path.GetRandomFileName());
            storagePath = Path.Combine(Path.GetRandomFileName());
            Directory.CreateDirectory(tempPath);
            Directory.CreateDirectory(storagePath);
            daemon = new VSIX.SonarLintDaemon(settings, logger, VSIX.SonarLintDaemon.daemonVersion, storagePath, tempPath);
        }

        [TestMethod]
        public void Not_Installed()
        {
            daemon.IsInstalled.Should().BeFalse();
            daemon.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Run_Without_Install()
        {
            daemon.Start();
        }

        [TestMethod]
        public void Stop_Without_Start_Has_No_Effect()
        {
            daemon.IsRunning.Should().BeFalse(); // Sanity test
            daemon.Stop();
            daemon.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        [Ignore]
        public void Install_Reinstall_Run()
        {
            daemon.Install();
            Directory.GetFiles(tempPath).Length.Should().Be(1);
            Directory.GetDirectories(storagePath).Length.Should().Be(1);
            Assert.IsTrue(daemon.IsInstalled);
            Assert.IsFalse(daemon.IsRunning);

            daemon.Install();
            Directory.GetFiles(tempPath).Length.Should().Be(1);
            Directory.GetDirectories(storagePath).Length.Should().Be(1);
            daemon.IsInstalled.Should().BeTrue();
            daemon.IsRunning.Should().BeFalse();

            daemon.Start();
            daemon.IsInstalled.Should().BeTrue();
            daemon.IsRunning.Should().BeTrue();
            daemon.Stop();
            daemon.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        public void SystemInteractiveAsync_EnsureCorrectVersion()
        {
            // Regression test for https://github.com/SonarSource/sonarlint-visualstudio/issues/850

            // We're just checking for the expected hard-coded assembly versions. This test will need
            // to be updated if the version of Grpc.Core being used changes.

            var grpcCoreAsm = AssemblyHelper.GetVersionOfReferencedAssembly(typeof(VSIX.SonarLintDaemon), "Grpc.Core");
            grpcCoreAsm.Should().NotBeNull("Cannot locate the Grpc.Core assembly referenced by SonarLint");
            grpcCoreAsm.Should().Be(new Version(1, 0, 0, 0),
                "SonarLint not referencing the expected version of Grpc.Core. Does this test need to be updated?");

            var siaAsm = AssemblyHelper.GetVersionOfReferencedAssembly(typeof(VSIX.SonarLintDaemon), "System.Interactive.Async");
            siaAsm.Should().Be(new Version("3.0.1000.0"),
                "SonarLint is not using the version of System.Interactive.Async expected by Grpc.Core. This will cause a runtime error.");
        }

        [TestMethod]
        public void SafeOperation_NonCriticalException()
        {
            // Act
            daemon.SafeOperation(() => { throw new InvalidCastException("YYY"); } );

            // Assert
            logger.AssertPartialOutputStringExists("System.InvalidCastException", "YYY");
        }

        [TestMethod]
        public void SafeOperation_CriticalExceptionsAreNotCaught()
        {
            // Arrange
            Action op = () =>
            {
                daemon.SafeOperation(() => { throw new StackOverflowException(); });
            };

            // Act and assert
            op.Should().ThrowExactly<StackOverflowException>();
            logger.AssertNoOutputMessages();
        }

        [TestCleanup]
        public void CleanUp()
        {
            ForceDeleteDirectory(tempPath);
            ForceDeleteDirectory(storagePath);
        }

        private static void ForceDeleteDirectory(string path)
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Thread.Sleep(1);
                Directory.Delete(path, true);
            }
        }
    }
}
