﻿/*
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
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.NodeJSLocator;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.EslintBridgeClient
{
    [TestClass]
    public class EslintBridgeStartUpTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<EslintBridgeStartUp, IEslintBridgeStartUp>(null, new[]
            {
                MefTestHelpers.CreateExport<string>("some path", EslintBridgeStartUp.EslintBridgeDirectoryMefContractName),
                MefTestHelpers.CreateExport<INodeLocator>(Mock.Of<INodeLocator>()),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>())
            });
        }

        [TestMethod]
        public void Start_FailsToFindNode_FileNotFoundException()
        {
            var nodeLocator = SetupNodeLocator(null);

            var testSubject = CreateTestSubject(nodeLocator: nodeLocator.Object);
            Func<Task> act = async () => await testSubject.Start();

            act.Should().ThrowExactly<FileNotFoundException>().And.Message.Should().Contain("node.exe");
        }

        [TestMethod]
        public void Start_FailsToFindStartupScript_FileNotFoundException()
        {
            const string filePath = "somefile.txt";

            var fileSystem = SetupStartupScriptFile(filePath, false);
            var testSubject = CreateTestSubject(filePath, fileSystem: fileSystem.Object);
            Func<Task> act = async () => await testSubject.Start();

            act.Should().ThrowExactly<FileNotFoundException>().And.Message.Should().Contain(filePath);
        }

        [TestMethod]
        public async Task Start_ServerStarts_ReturnsPortNumber()
        {
            var scriptPath = CreateScriptThatPrintsPortNumber(123);
            var startupScriptPath = " "; // pass whitespace so it would not fail NullOrEmpty validation but also do nothing

            var nodeLocator = SetupNodeLocator(scriptPath);
            var testSubject = CreateTestSubject(startupScriptPath, nodeLocator: nodeLocator.Object);

            var port = await testSubject.Start();
            port.Should().Be(123);
        }

        [TestMethod]
        public async Task Start_TaskSucceeds_NextCallDoesNotStartAnotherServer()
        {
            var scriptPath = CreateScriptThatPrintsPortNumber(123);
            var startupScriptPath = " "; // pass whitespace so it would not fail NullOrEmpty validation but also do nothing

            var nodeLocator = SetupNodeLocator(scriptPath);
            var testSubject = CreateTestSubject(startupScriptPath, nodeLocator: nodeLocator.Object);

            var port = await testSubject.Start();
            port.Should().Be(123);

            port = await testSubject.Start();
            port.Should().Be(123);

            nodeLocator.Verify(x => x.Locate(), Times.Once);
        }

        [TestMethod]
        public async Task Start_TaskFails_NextCallAttemptsAgain()
        {
            var scriptPath = CreateScriptThatPrintsPortNumber(123);
            var startupScriptPath = " "; // pass whitespace so it would not fail NullOrEmpty validation but also do nothing

            var nodeLocator = new Mock<INodeLocator>();
            nodeLocator.SetupSequence(x => x.Locate())
                .Throws(new NotImplementedException("some exception"))
                .Returns(scriptPath);

            var testSubject = CreateTestSubject(startupScriptPath, nodeLocator: nodeLocator.Object);

            Func<Task> act = async () => await testSubject.Start();
            act.Should().Throw<NotImplementedException>().And.Message.Should().Be("some exception");

            var port = await testSubject.Start();
            port.Should().Be(123);
        }

        [TestMethod]
        public async Task Dispose_KillsRunningProcess()
        {
            var scriptPath = CreateScriptThatPrintsPortNumber(123, isHanging: true);
            var startupScriptPath = " "; // pass whitespace so it would not fail NullOrEmpty validation but also do nothing

            var nodeLocator = SetupNodeLocator(scriptPath);
            var testSubject = CreateTestSubject(startupScriptPath, nodeLocator: nodeLocator.Object);

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
            var scriptPath = CreateScriptThatPrintsPortNumber(123, isHanging: true);
            var startupScriptPath = " "; // pass whitespace so it would not fail NullOrEmpty validation but also do nothing

            var nodeLocator = SetupNodeLocator(scriptPath);
            var testSubject = CreateTestSubject(startupScriptPath, nodeLocator: nodeLocator.Object);

            await testSubject.Start();
            testSubject.Process.HasExited.Should().BeFalse();

            testSubject.Dispose();
            testSubject.Process.Should().BeNull();

            Action act = () => testSubject.Dispose();
            act.Should().NotThrow();

            testSubject.Process.Should().BeNull();
        }

        private static Mock<INodeLocator> SetupNodeLocator(string nodePath)
        {
            var nodeLocator = new Mock<INodeLocator>();
            nodeLocator.Setup(x => x.Locate()).Returns(nodePath);

            return nodeLocator;
        }

        private EslintBridgeStartUp CreateTestSubject(string startupScriptPath = null, INodeLocator nodeLocator = null, IFileSystem fileSystem = null, ILogger logger = null)
        {
            startupScriptPath ??= "somefile.txt";
            nodeLocator ??= SetupNodeLocator("some path").Object;
            fileSystem ??= SetupStartupScriptFile(startupScriptPath, true).Object;
            logger ??= Mock.Of<ILogger>();

            return new EslintBridgeStartUp(startupScriptPath, nodeLocator, fileSystem, logger);
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
    }
}
