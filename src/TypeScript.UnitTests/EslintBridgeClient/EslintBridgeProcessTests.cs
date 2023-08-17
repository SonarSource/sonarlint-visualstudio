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
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Threading;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Core.JsTs;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.EslintBridgeClient
{
    [TestClass]
    public class EslintBridgeProcessTests
    {
        [TestMethod]
        public void Start_FailsToFindNode_EslintBridgeProcessLaunchException()
        {
            var nodeLocator = SetupNodeLocator(null);

            var testSubject = CreateTestSubject(compatibleNodeLocator: nodeLocator.Object);
            Func<Task> act = async () => await testSubject.Start();

            act.Should().ThrowExactly<EslintBridgeProcessLaunchException>().And.Message.Should().Contain("node.exe");
        }

        [TestMethod] // Regression test for #2370
        public async Task Start_ServerPathIsEscaped()
        {
            var fakeNodeExePath = CreateScriptThatPrintsPortNumber(123, isHanging: false);
            var startupScriptPath = "dummy path";

            var nodeLocator = SetupNodeLocator(fakeNodeExePath);
            var testSubject = CreateTestSubject(startupScriptPath, compatibleNodeLocator: nodeLocator.Object);

            Process spawnedProcess = null;
            try
            {
                await testSubject.Start();
                spawnedProcess = testSubject.Process;

                spawnedProcess.StartInfo.Arguments.Should().Be("\"dummy path\"" + GetDefaultParameters());
            }
            finally
            {
                SafeKillProcess(spawnedProcess);
            }
        }

        [TestMethod]
        public async Task Start_SecondCall_ProcessFailed_StartsNewProcess()
        {
            var fakeNodeExePath = CreateScriptThatPrintsPortNumber(123, isHanging: true);
            var startupScriptPath = "dummy path";

            var nodeLocator = SetupNodeLocator(fakeNodeExePath);
            var testSubject = CreateTestSubject(startupScriptPath, compatibleNodeLocator: nodeLocator.Object);

            Process lastSpawnedProcess = null;
            try
            {
                await testSubject.Start();

                var oldProcess = testSubject.Process;
                lastSpawnedProcess = oldProcess;

                oldProcess.HasExited.Should().BeFalse();
                oldProcess.Kill();
                await oldProcess.WaitForExitAsync();
                oldProcess.HasExited.Should().BeTrue();

                await testSubject.Start();

                var newProcess = testSubject.Process;
                lastSpawnedProcess = newProcess;
                newProcess.Should().NotBeSameAs(oldProcess);
                newProcess.HasExited.Should().BeFalse();
            }
            finally
            {
                SafeKillProcess(lastSpawnedProcess);
            }
        }

        [TestMethod]
        public async Task Start_SecondCall_ProcessIsStillRunning_DoesNotStartNewProcess()
        {
            var fakeNodeExePath = CreateScriptThatPrintsPortNumber(123, isHanging: true);
            var startupScriptPath = "dummy path";

            var nodeLocator = SetupNodeLocator(fakeNodeExePath);
            var testSubject = CreateTestSubject(startupScriptPath, compatibleNodeLocator: nodeLocator.Object);

            Process lastSpawnedProcess = null;
            try
            {
                await testSubject.Start();

                var oldProcess = testSubject.Process;
                lastSpawnedProcess = oldProcess;

                oldProcess.HasExited.Should().BeFalse();

                await testSubject.Start();

                var newProcess = testSubject.Process;
                lastSpawnedProcess = newProcess;

                newProcess.Should().BeSameAs(oldProcess);
                newProcess.HasExited.Should().BeFalse();
            }
            finally
            {
                SafeKillProcess(lastSpawnedProcess);
            }
        }

        [TestMethod]
        public void Start_FailsToFindStartupScript_EslintBridgeProcessLaunchException()
        {
            const string filePath = "somefile.txt";

            var fileSystem = SetupStartupScriptFile(filePath, false);
            var testSubject = CreateTestSubject(filePath, fileSystem: fileSystem.Object);
            Func<Task> act = async () => await testSubject.Start();

            act.Should().ThrowExactly<EslintBridgeProcessLaunchException>().And.Message.Should().Contain(filePath);
        }

        [TestMethod]
        public async Task Start_ServerStarts_ReturnsPortNumber()
        {
            var fakeNodeExePath = CreateScriptThatPrintsPortNumber(123);
            var startupScriptPath = "dummy path";

            var nodeLocator = SetupNodeLocator(fakeNodeExePath);
            var testSubject = CreateTestSubject(startupScriptPath, compatibleNodeLocator: nodeLocator.Object);

            var result = await testSubject.Start();
            result.Should().Be(123);
        }

        [TestMethod]
        public async Task Start_TaskSucceeds_NextCallDoesNotStartAnotherServer()
        {
            var fakeNodeExePath = CreateScriptThatPrintsPortNumber(123);
            var startupScriptPath = "dummy path";

            var nodeLocator = SetupNodeLocator(fakeNodeExePath);
            var testSubject = CreateTestSubject(startupScriptPath, compatibleNodeLocator: nodeLocator.Object);

            var result = await testSubject.Start();
            result.Should().Be(123);

            result = await testSubject.Start();
            result.Should().Be(123);

            nodeLocator.Verify(x => x.Locate(), Times.Once);
        }

        [TestMethod]
        public async Task Start_TaskFails_NextCallAttemptsAgain()
        {
            var fakeNodeExePath = CreateScriptThatPrintsPortNumber(123);
            var startupScriptPath = "dummy path";

            var nodeLocator = new Mock<ICompatibleNodeLocator>();
            nodeLocator.SetupSequence(x => x.Locate())
                .Throws(new NotImplementedException("some exception"))
                .Returns(new NodeVersionInfo(fakeNodeExePath, new Version()));

            var testSubject = CreateTestSubject(startupScriptPath, compatibleNodeLocator: nodeLocator.Object);

            Func<Task> act = async () => await testSubject.Start();
            act.Should().Throw<NotImplementedException>().And.Message.Should().Be("some exception");

            var result = await testSubject.Start();
            result.Should().Be(123);
        }

        [TestMethod]
        public async Task Dispose_KillsRunningProcess()
        {
            var fakeNodeExePath = CreateScriptThatPrintsPortNumber(123, isHanging: true);
            var startupScriptPath = "dummy path";

            var nodeLocator = SetupNodeLocator(fakeNodeExePath);
            var testSubject = CreateTestSubject(startupScriptPath, compatibleNodeLocator: nodeLocator.Object);

            await testSubject.Start();

            testSubject.Process.HasExited.Should().BeFalse();

            testSubject.Dispose();

            testSubject.Process.Should().BeNull();
        }

        [TestMethod]
        public void Dispose_ProcessNotStarted_NoException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Dispose();
            act.Should().NotThrow();

            testSubject.Process.Should().BeNull();
        }

        [TestMethod]
        public async Task Dispose_ProcessAlreadyKilled_NoException()
        {
            var fakeNodeExePath = CreateScriptThatPrintsPortNumber(123, isHanging: true);
            var startupScriptPath = "dummy path";

            var nodeLocator = SetupNodeLocator(fakeNodeExePath);
            var testSubject = CreateTestSubject(startupScriptPath, compatibleNodeLocator: nodeLocator.Object);

            await testSubject.Start();
            testSubject.Process.HasExited.Should().BeFalse();

            testSubject.Dispose();
            testSubject.Process.Should().BeNull();

            Action act = () => testSubject.Dispose();
            act.Should().NotThrow();

            testSubject.Process.Should().BeNull();
        }

        [TestMethod]
        public void IsRunning_ServerWasNeverStarted_False()
        {
            var testSubject = CreateTestSubject();

            testSubject.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        public void IsRunning_ServerWasNeverStartedAndWasDisposed_False()
        {
            var testSubject = CreateTestSubject();

            testSubject.Dispose();

            testSubject.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        public async Task IsRunning_ServerWasRestarted_True()
        {
            var fakeNodeExePath = CreateScriptThatPrintsPortNumber(123, isHanging: true);
            var startupScriptPath = "dummy path";

            var nodeLocator = SetupNodeLocator(fakeNodeExePath);
            var testSubject = CreateTestSubject(startupScriptPath, compatibleNodeLocator: nodeLocator.Object);

            Process spawnedProcess = null;
            try
            {
                await testSubject.Start();

                testSubject.IsRunning.Should().BeTrue();

                testSubject.Dispose();

                testSubject.IsRunning.Should().BeFalse();

                await testSubject.Start();
                spawnedProcess = testSubject.Process;

                testSubject.IsRunning.Should().BeTrue();
            }
            finally
            {
                SafeKillProcess(spawnedProcess);
            }
        }

        private static Mock<ICompatibleNodeLocator> SetupNodeLocator(string nodePath)
        {
            var version = string.IsNullOrEmpty(nodePath) ? null : new NodeVersionInfo(nodePath, new Version());
            var nodeLocator = new Mock<ICompatibleNodeLocator>();
            nodeLocator.Setup(x => x.Locate()).Returns(version);

            return nodeLocator;
        }

        private EslintBridgeProcess CreateTestSubject(string startupScriptPath = null, ICompatibleNodeLocator compatibleNodeLocator = null, IFileSystem fileSystem = null, ILogger logger = null)
        {
            startupScriptPath ??= "somefile.txt";
            compatibleNodeLocator ??= SetupNodeLocator("some path").Object;
            fileSystem ??= SetupStartupScriptFile(startupScriptPath, true).Object;
            logger ??= Mock.Of<ILogger>();

            return new EslintBridgeProcess(startupScriptPath, compatibleNodeLocator, fileSystem, logger);
        }

        private static Mock<IFileSystem> SetupStartupScriptFile(string startupScriptPath, bool exists)
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(startupScriptPath)).Returns(exists);

            return fileSystem;
        }

        private string CreateScriptThatPrintsPortNumber(int portNumber, bool isHanging = false)
        {
            var content = "echo port " + portNumber;

            if (isHanging)
            {
                content += Environment.NewLine + "set /p asd=\"Waiting for process to be killed\"";
            }

            var fileName = Path.GetTempFileName() + ".bat";

            File.WriteAllText(fileName, content);

            return fileName;
        }

        private static void SafeKillProcess(Process process)
        {
            try
            {
                // Kill spawned process
                process?.Kill();
            }
            catch
            {
                // do nothing
            }
        }

        private static string GetDefaultParameters()
        {
            var workDir = PathHelper.GetTempDirForTask(true, "ESLintBridge", "workdir");

            //To pass the sonarlint parameter we have to pass all the parameters before 
            //Commandline interface for eslintbridge is not accepting named parameters  
            return $" \"0\" \"127.0.0.1\" \"{workDir}\" \"true\" \"true\"";
        }
    }
}
