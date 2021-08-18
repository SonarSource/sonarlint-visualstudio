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
using System.Diagnostics;
using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.CFamily.SystemAbstractions;
using SonarLint.VisualStudio.Integration.UnitTests.Helpers;

namespace SonarLint.VisualStudio.CFamily.UnitTests.SystemAbstractions
{
    [TestClass]
    public class ProcessWrapperTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void ProcessFactory_Start_ReturnsWrappedProcess()
        {
            var exeName = WriteBatchFileForTest(TestContext, "@echo hello world");

            IProcess wrappedProcess = null;
            var processStartInfo = CreateProcessStartInfo(exeName);
            var testSubject = new ProcessFactory();

            try
            {
                wrappedProcess = testSubject.Start(processStartInfo);

                wrappedProcess.Should().NotBeNull();
                wrappedProcess.StartInfo.Should().BeSameAs(processStartInfo);
            }
            finally
            {
                if (!wrappedProcess.HasExited)
                {
                    wrappedProcess?.Kill();
                }
            }
        }

        [TestMethod]
        public void Execute_PropertiesReturnExpectedValue()
        {
            var exeName = WriteBatchFileForTest(TestContext,
@"@echo Hello world
exit -2
");

            using var processScope = StartProcess(exeName);
            var testSubject = new ProcessWrapper(processScope.Process);

            // Act
            testSubject.WaitForExit(1000);

            testSubject.Id.Should().Be(processScope.Process.Id);
            testSubject.HasExited.Should().Be(processScope.Process.HasExited);
            testSubject.ExitCode.Should().Be(-2);
            testSubject.StartInfo.Should().BeSameAs(processScope.Process.StartInfo);
        }

        [TestMethod]
        public void Execute_OutputIsForwarded()
        {
            var exeName = WriteBatchFileForTest(TestContext,
@"@echo Hello world
@echo Text written to error output should not be captured >&2
@echo xxx yyy
");

            var sb = new StringBuilder();

            using var processScope = StartProcess(exeName);
            var testSubject = new ProcessWrapper(processScope.Process);

            // Act
            testSubject.HandleOutputDataReceived = data => sb.AppendLine(data);
            testSubject.BeginOutputReadLine();
            testSubject.WaitForExit(2000);

            // Assert
            testSubject.ExitCode.Should().Be(0);

            var output = sb.ToString();
            output.Should().Contain("Hello world");
            output.Should().Contain("xxx yyy");
            output.Should().NotContain("Testing 1,2,3...");
        }

        [TestMethod]
        public void Execute_ProcessTimesOut_CanBeKilled()
        {
            // Arrange
            var exeName = WriteBatchFileForTest(TestContext,
@"@echo Waiting for keyboard input which will not arrive...
set /p arg=
");

            using var processScope = StartProcess(exeName);
            var testSubject = new ProcessWrapper(processScope.Process);

            // Should timeout be because the batch file is waiting for input...
            testSubject.WaitForExit(300);
            testSubject.HasExited.Should().Be(false);
            CheckProcessIsRunning(testSubject.Id);

            // Now kill the process
            testSubject.Kill();
            testSubject.HasExited.Should().Be(true);
            testSubject.ExitCode.Should().Be(-1);
            CheckProcessIsNotRunning(testSubject.Id);
        }

        [TestMethod]
        public void Execute_ProcessTimesOut_CanBeDisposed()
        {
            // Arrange
            var exeName = WriteBatchFileForTest(TestContext,
@"@echo Waiting for keyboard input which will not arrive...
set /p arg=
");
            using var processScope = StartProcess(exeName);

            var testSubject = new ProcessWrapper(processScope.Process);

            // Should timeout be because the batch file is waiting for input...
            testSubject.WaitForExit(300);
            testSubject.HasExited.Should().Be(false);
            CheckProcessIsRunning(testSubject.Id);

            int id = testSubject.Id; // shouldn't be able to fetch this once disposed
            // Now dispose the process
            testSubject.Dispose();

            CheckOperationOnDisposedObjectFails(() => _ = testSubject.HasExited);
            CheckOperationOnDisposedObjectFails(() => _ = testSubject.ExitCode);

            CheckProcessIsRunning(id); // somewhat unexpectedly, Dispose does not kill the running process
        }

        private static ProcessScope StartProcess(string exeName) =>
            new ProcessScope(Process.Start(CreateProcessStartInfo(exeName)));

        private static ProcessStartInfo CreateProcessStartInfo(string exeName) =>
            new ProcessStartInfo
            {
                FileName = exeName,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = false
            };

        /// <summary>
        /// Creates a batch file with the name of the current test
        /// </summary>
        /// <returns>Returns the full file name of the new file</returns>
        private static string WriteBatchFileForTest(TestContext context, string content)
        {
            var testPath = CreateTestSpecificFolder(context);
            var fileName = Path.Combine(testPath, context.TestName + ".bat");
            File.Exists(fileName).Should().BeFalse("Not expecting a batch file to already exist: {0}", fileName);
            File.WriteAllText(fileName, content);
            return fileName;
        }

        private static string CreateTestSpecificFolder(TestContext testContext)
        {
            var testPath = Path.Combine(testContext.DeploymentDirectory, testContext.TestName);

            if (!Directory.Exists(testPath))
            {
                Directory.CreateDirectory(testPath);
            }
            return testPath;
        }

        private static void CheckProcessIsRunning(int id) =>
            Process.GetProcessById(id).Should().NotBeNull();

        private static void CheckProcessIsNotRunning(int id)
        {
            Action act = () => Process.GetProcessById(id);
            act.Should().ThrowExactly<ArgumentException>();
        }

        private void CheckOperationOnDisposedObjectFails(Action op) =>
            op.Should().ThrowExactly<InvalidOperationException>();
    }
}
