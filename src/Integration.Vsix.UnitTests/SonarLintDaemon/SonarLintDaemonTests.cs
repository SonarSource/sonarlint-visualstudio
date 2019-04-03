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
using VSIX = SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SonarLintDaemonTests
    {
        private string tempPath;
        private string storagePath;
        private TestableSonarLintDaemon testableDaemon;
        private TestLogger logger;
        private ConfigurableSonarLintSettings settings;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void SetUp()
        {
            settings = new ConfigurableSonarLintSettings
            {
                DaemonLogLevel = DaemonLogLevel.Verbose
            };
            logger = new TestLogger();

            tempPath = Path.Combine(TestContext.DeploymentDirectory, Path.GetRandomFileName());
            storagePath = Path.Combine(TestContext.DeploymentDirectory, Path.GetRandomFileName());
            Directory.CreateDirectory(tempPath);
            Directory.CreateDirectory(storagePath);
            testableDaemon = new TestableSonarLintDaemon(settings, logger, VSIX.SonarLintDaemon.daemonVersion, storagePath, tempPath);
            
        }

        [TestCleanup]
        public void CleanUp()
        {
            ForceDeleteDirectory(tempPath);
            ForceDeleteDirectory(storagePath);
        }


        [TestMethod]
        public void Not_Installed()
        {
            testableDaemon.IsInstalled.Should().BeFalse();
            testableDaemon.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        public void Run_Without_Install()
        {
            Action op = () => testableDaemon.Start();

            op.Should().ThrowExactly<InvalidOperationException>();
        }

        [TestMethod]
        public void Stop_Without_Start_Has_No_Effect()
        {
            testableDaemon.IsRunning.Should().BeFalse(); // Sanity test
            testableDaemon.Stop();
            testableDaemon.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        [Ignore]
        public void Install_Reinstall_Run()
        {
            testableDaemon.Install();
            Directory.GetFiles(tempPath).Length.Should().Be(1);
            Directory.GetDirectories(storagePath).Length.Should().Be(1);
            Assert.IsTrue(testableDaemon.IsInstalled);
            Assert.IsFalse(testableDaemon.IsRunning);

            testableDaemon.Install();
            Directory.GetFiles(tempPath).Length.Should().Be(1);
            Directory.GetDirectories(storagePath).Length.Should().Be(1);
            testableDaemon.IsInstalled.Should().BeTrue();
            testableDaemon.IsRunning.Should().BeFalse();

            testableDaemon.Start();
            testableDaemon.IsInstalled.Should().BeTrue();
            testableDaemon.IsRunning.Should().BeTrue();
            testableDaemon.Stop();
            testableDaemon.IsRunning.Should().BeFalse();
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
            testableDaemon.SafeOperation(() => { throw new InvalidCastException("YYY"); } );

            // Assert
            logger.AssertPartialOutputStringExists("System.InvalidCastException", "YYY");
        }

        [TestMethod]
        public void SafeOperation_CriticalExceptionsAreNotCaught()
        {
            // Arrange
            Action op = () =>
            {
                testableDaemon.SafeOperation(() => { throw new StackOverflowException(); });
            };

            // Act and assert
            op.Should().ThrowExactly<StackOverflowException>();
            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void HandleOutputDataReceived_NoData_NoError()
        {
            // Act
            testableDaemon.HandleOutputDataReceived(null);

            // Assert
            logger.AssertNoOutputMessages();
            testableDaemon.CreateChannelCallCount.Should().Be(0);
        }

        [TestMethod]
        public void HandleOutputDataReceived_Data_NotServerStarted_Verbose_LoggedButCreateChannelIsNotCalled()
        {
            // Act
            settings.DaemonLogLevel = DaemonLogLevel.Verbose;
            testableDaemon.HandleOutputDataReceived("Something happened...");

            // Assert
            logger.AssertOutputStringExists("Something happened...");
            testableDaemon.CreateChannelCallCount.Should().Be(0);
            testableDaemon.WasReadyEventInvoked.Should().BeFalse();
        }

        [TestMethod]
        public void HandleOutputDataReceived_Data_NotServerStarted_NotVerbose_NotLogged()
        {
            // Act
            settings.DaemonLogLevel = DaemonLogLevel.Info;
            testableDaemon.HandleOutputDataReceived("Data should only be logger for logging level is verbose");

            // Assert
            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void HandleOutputDataReceived_Data_ServerStarted_CreateChannelIsCalled()
        {
            // Act
            testableDaemon.HandleOutputDataReceived("XXXServer startedyyyy");

            // Assert
            logger.AssertOutputStringExists("XXXServer startedyyyy");
            testableDaemon.CreateChannelCallCount.Should().Be(1);
            testableDaemon.WasReadyEventInvoked.Should().BeTrue();
            testableDaemon.WasStopCalled.Should().BeFalse();
        }

        [TestMethod]
        public void HandleOutputDataReceived_Data_ServerStarted_NonCriticalException_StopIsCalled()
        {            
            // Throw a non-critical exception when 
            testableDaemon.CreateChannelAndStreamLogsOp = () => { throw new InvalidCastException(); };

            // Act
            testableDaemon.HandleOutputDataReceived("Server started");

            // Assert
            logger.AssertOutputStringExists("Server started");
            testableDaemon.CreateChannelCallCount.Should().Be(1);
            testableDaemon.WasReadyEventInvoked.Should().BeFalse();
            logger.AssertPartialOutputStringExists("System.InvalidCastException");
            testableDaemon.WasStopCalled.Should().BeTrue();
        }

        [TestMethod]
        public void HandleOutputDataReceived_Data_ServerStarted_CriticalException_StopIsCalled()
        {
            // Throw a critical exception when 
            testableDaemon.CreateChannelAndStreamLogsOp = () => { throw new StackOverflowException(); };

            Action op = () => testableDaemon.HandleOutputDataReceived("Server started");

            // Act
            op.Should().ThrowExactly<StackOverflowException>();

            // Assert
            logger.AssertOutputStringExists("Server started");
            testableDaemon.CreateChannelCallCount.Should().Be(1);
            testableDaemon.WasReadyEventInvoked.Should().BeFalse();

            // We should not do any further processing for a critical exception
            // -> not expecting the daemon to have been stopped
            testableDaemon.WasStopCalled.Should().BeFalse();
        }

        [TestMethod]
        public void HandleErrorDataReceived_NoData_NoError()
        {
            // Act
            testableDaemon.HandleErrorDataReceived(null);

            // Assert
            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void HandleErrorDataReceived_HasData_IsLogged()
        {
            // 1. Verbose logging
            settings.DaemonLogLevel = DaemonLogLevel.Verbose;
            testableDaemon.HandleErrorDataReceived("error - verbose");

            logger.AssertOutputStringExists("error - verbose");


            // 2. Minimal logging - errors still logged
            settings.DaemonLogLevel = DaemonLogLevel.Minimal;
            testableDaemon.HandleErrorDataReceived("error - minimal");

            logger.AssertOutputStringExists("error - minimal");
        }

        [TestMethod]
        public void Dispose_WorkingDirectoryDeleted()
        {
            // Sanity check
            testableDaemon.WorkingDirectory.Should().NotBeNull();
            Directory.Exists(testableDaemon.WorkingDirectory).Should().BeTrue();

            // 1. Dispose -> directory cleared
            testableDaemon.Dispose();
            Directory.Exists(testableDaemon.WorkingDirectory).Should().BeFalse();

            // 2. Multiple dispose should not error
            testableDaemon.Dispose();
        }

        [TestMethod]
        public void Analyze()
        {

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

        private class TestableSonarLintDaemon : VSIX.SonarLintDaemon
        {
            public TestableSonarLintDaemon(ISonarLintSettings settings, ILogger logger, string version, string storagePath, string tmpPath)
                : base(settings, logger, version, storagePath, tmpPath)
            {
                this.Ready += (s, a) => WasReadyEventInvoked = true;
            }

            public bool WasReadyEventInvoked { get; private set; }

            public bool WasStopCalled { get; private set; }

            public Action CreateChannelAndStreamLogsOp { get; set; }

            public int CreateChannelCallCount { get; private set; }

            protected override void CreateChannelAndStreamLogs()
            {
                CreateChannelCallCount++;
                CreateChannelAndStreamLogsOp?.Invoke();
            }

            public override void Stop()
            {
                WasStopCalled = true;
                base.Stop();
            }
        }
    }
}
