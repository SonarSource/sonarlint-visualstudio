﻿/*
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.CFamily.Helpers.UnitTests;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.CFamily.SubProcess.UnitTests
{
    [TestClass]
    public class ProcessRunnerTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Execute_WhenRunnerArgsIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            Action action = () => new ProcessRunner(new ConfigurableSonarLintSettings(), new TestLogger()).Execute(null);

            // Act & Assert
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("runnerArgs");
        }

        [TestMethod]
        public void ProcRunner_ExecutionFailed()
        {
            // Arrange
            var exeName = WriteBatchFileForTest(TestContext, "exit -2");

            var logger = new TestLogger();
            var args = new ProcessRunnerArguments(exeName, true);
            var runner = CreateProcessRunner(logger);

            // Act
            runner.Execute(args);

            // Assert
            runner.ExitCode.Should().Be(-2, "Unexpected exit code");
        }

        [TestMethod]
        public void ProcRunner_ExecutionSucceeded()
        {
            // Arrange
            var exeName = WriteBatchFileForTest(TestContext,
@"@echo Hello world
@echo xxx yyy
@echo Testing 1,2,3...>&2
");

            var logger = new TestLogger();
            var args = new ProcessRunnerArguments(exeName, true);
            var output = "";
            args.HandleOutputStream = reader => output = reader.ReadToEnd();
            var runner = CreateProcessRunner(logger);

            // Act
            runner.Execute(args);

            // Assert
            runner.ExitCode.Should().Be(0, "Unexpected exit code");

            output.Should().Contain("Hello world");
            output.Should().NotContain("Testing 1,2,3...");
        }

        [TestMethod]
        public void ProcRunner_PassesEnvVariables()
        {
            // Arrange
            var logger = new TestLogger();
            var runner = CreateProcessRunner(logger);

            var exeName = WriteBatchFileForTest(TestContext,
@"echo %PROCESS_VAR%
@echo %PROCESS_VAR2%
@echo %PROCESS_VAR3%
");
            var envVariables = new Dictionary<string, string>() {
                { "PROCESS_VAR", "PROCESS_VAR value" },
                { "PROCESS_VAR2", "PROCESS_VAR2 value" },
                { "PROCESS_VAR3", "PROCESS_VAR3 value" } };

            var args = new ProcessRunnerArguments(exeName, true)
            {
                EnvironmentVariables = envVariables
            };
            var output = "";
            args.HandleOutputStream = reader => output = reader.ReadToEnd();
            // Act
            runner.Execute(args);

            // Assert
            runner.ExitCode.Should().Be(0, "Unexpected exit code");

            output.Should().Contain("PROCESS_VAR value");
            output.Should().Contain("PROCESS_VAR2 value");
            output.Should().Contain("PROCESS_VAR3 value");
        }

        [TestMethod]
        public void ProcRunner_PassesEnvVariables_OverrideExisting()
        {
            // Tests that existing environment variables will be overwritten successfully

            // Arrange
            var logger = new TestLogger();
            var runner = CreateProcessRunner(logger);
            var output = "";

            try
            {
                // It's possible the user won't be have permissions to set machine level variables
                // (e.g. when running on a build agent). Carry on with testing the other variables.
                SafeSetEnvironmentVariable("proc.runner.test.machine", "existing machine value", EnvironmentVariableTarget.Machine, logger);
                Environment.SetEnvironmentVariable("proc.runner.test.process", "existing process value", EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("proc.runner.test.user", "existing user value", EnvironmentVariableTarget.User);

                var exeName = WriteBatchFileForTest(TestContext,
@"@echo file: %proc.runner.test.machine%
@echo file: %proc.runner.test.process%
@echo file: %proc.runner.test.user%
");

                var envVariables = new Dictionary<string, string>() {
                    { "proc.runner.test.machine", "machine override" },
                    { "proc.runner.test.process", "process override" },
                    { "proc.runner.test.user", "user override" } };

                var args = new ProcessRunnerArguments(exeName, true)
                {
                    EnvironmentVariables = envVariables,
                    HandleOutputStream = reader =>
                    {
                        output = reader.ReadToEnd();
                    }
                };

                // Act
                runner.Execute(args);

                // Assert
                runner.ExitCode.Should().Be(0, "Unexpected exit code");
            }
            finally
            {
                SafeSetEnvironmentVariable("proc.runner.test.machine", null, EnvironmentVariableTarget.Machine, logger);
                Environment.SetEnvironmentVariable("proc.runner.test.process", null, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("proc.runner.test.user", null, EnvironmentVariableTarget.User);
            }

            // Check the child process used expected values
            output.Should().Contain("file: machine override");
            output.Should().Contain("file: process override");
            output.Should().Contain("file: user override");

            // Check the runner reported it was overwriting existing variables
            // Note: the existing non-process values won't be visible to the child process
            // unless they were set *before* the test host launched, which won't be the case.
            logger.AssertSingleDebugMessageExists("proc.runner.test.process", "existing process value", "process override");
        }

        [TestMethod]
        public void ProcRunner_MissingExe()
        {
            // Tests attempting to launch a non-existent exe

            // Arrange
            var logger = new TestLogger();
            var args = new ProcessRunnerArguments("missingExe.foo", false);
            var runner = CreateProcessRunner(logger);

            // Act
            runner.Execute(args);

            // Assert
            runner.ExitCode.Should().Be(ProcessRunner.ErrorCode, "Unexpected exit code");
            logger.AssertSingleErrorExists("missingExe.foo");
        }

        [TestMethod]
        public void ProcRunner_ArgumentQuoting()
        {
            // Checks arguments passed to the child process are correctly quoted

            // Arrange
            var testDir = CreateTestSpecificFolder(TestContext);
            // Create a dummy exe that will produce a log file showing any input args
            var exeName = DummyExeHelper.CreateDummyExe(testDir, 0);

            var logger = new TestLogger();
            var args = new ProcessRunnerArguments(exeName, false);

            var expected = new[] {
                "unquoted",
                "\"quoted\"",
                "\"quoted with spaces\"",
                "/test:\"quoted arg\"",
                "unquoted with spaces",
                "quote in \"the middle",
                "quotes \"& ampersands",
                "\"multiple \"\"\"      quotes \" ",
                "trailing backslash \\",
                "all special chars: \\ / : * ? \" < > | %",
                "injection \" > foo.txt",
                "injection \" & echo haha",
                "double escaping \\\" > foo.txt"
            };

            args.CmdLineArgs = expected;

            var runner = CreateProcessRunner(logger);

            // Act
            runner.Execute(args);

            // Assert
            runner.ExitCode.Should().Be(0, "Unexpected exit code");

            // Check that the public and private arguments are passed to the child process
            var exeLogFile = DummyExeHelper.AssertDummyExeLogExists(testDir, TestContext);
            DummyExeHelper.AssertExpectedLogContents(exeLogFile, expected);
        }

        [TestMethod]
        public void ProcRunner_ArgumentQuotingForwardedByBatchScript()
        {
            // Checks arguments passed to a batch script which itself passes them on are correctly escaped

            // Arrange
            var testDir = CreateTestSpecificFolder(TestContext);
            // Create a dummy exe that will produce a log file showing any input args
            var exeName = DummyExeHelper.CreateDummyExe(testDir, 0);

            var batchName = WriteBatchFileForTest(TestContext, "\"" + exeName + "\" %*");

            var logger = new TestLogger();
            var args = new ProcessRunnerArguments(batchName, true);

            var expected = new[] {
                "unquoted",
                "\"quoted\"",
                "\"quoted with spaces\"",
                "/test:\"quoted arg\"",
                "unquoted with spaces",
                "quote in \"the middle",
                "quotes \"& ampersands",
                "\"multiple \"\"\"      quotes \" ",
                "trailing backslash \\",
                "all special chars: \\ / : * ? \" < > | %",
                "injection \" > foo.txt",
                "injection \" & echo haha",
                "double escaping \\\" > foo.txt"
            };

            args.CmdLineArgs = expected;

            var runner = CreateProcessRunner(logger);

            // Act
            runner.Execute(args);

            // Assert
            runner.ExitCode.Should().Be(0, "Unexpected exit code");

            // Check that the public and private arguments are passed to the child process
            var exeLogFile = DummyExeHelper.AssertDummyExeLogExists(testDir, TestContext);
            DummyExeHelper.AssertExpectedLogContents(exeLogFile, expected);
        }

        [TestMethod]
        [WorkItem(126)] // Exclude secrets from log data: http://jira.sonarsource.com/browse/SONARMSBRU-126
        public void ProcRunner_DoNotLogSensitiveData()
        {
            // Arrange
            var testDir = CreateTestSpecificFolder(TestContext);
            // Create a dummy exe that will produce a log file showing any input args
            var exeName = DummyExeHelper.CreateDummyExe(testDir, 0);

            var logger = new TestLogger();

            // Public args - should appear in the log
            var publicArgs = new string[]
            {
                "public1",
                "public2",
                "/d:sonar.projectKey=my.key"
            };

            var sensitiveArgs = new string[] {
                // Public args - should appear in the log
                "public1", "public2", "/dmy.key=value",

                // Sensitive args - should not appear in the log
                "/d:sonar.password=secret data password",
                "/d:sonar.login=secret data login",
                "/d:sonar.jdbc.password=secret data db password",
                "/d:sonar.jdbc.username=secret data db user name",

                // Sensitive args - different cases -> exclude to be on the safe side
                "/d:SONAR.jdbc.password=secret data db password upper",
                "/d:sonar.PASSWORD=secret data password upper",

                // Sensitive args - parameter format is slightly incorrect -> exclude to be on the safe side
                "/dsonar.login =secret data key typo",
                "sonar.password=secret data password typo"
            };

            var allArgs = sensitiveArgs.Union(publicArgs).ToArray();

            var runnerArgs = new ProcessRunnerArguments(exeName, false)
            {
                CmdLineArgs = allArgs,

                // Specify the arguments we consider to be sensitive.
                // Note: this is a change from the S4MSB which has a hard-coded set of sensitive keys.
                SensitivePropertyKeys = new string[]
                {
                    "sonar.password", "sonar.login", "sonar.jdbc.password", "sonar.jdbc.username"
                }
            };

            var runner = CreateProcessRunner(logger);

            // Act
            runner.Execute(runnerArgs);

            // Assert
            runner.ExitCode.Should().Be(0, "Unexpected exit code");

            // Check public arguments are logged but private ones are not
            foreach (var arg in publicArgs)
            {
                logger.AssertSingleDebugMessageExists(arg);
            }

            logger.AssertSingleDebugMessageExists("<sensitive data removed>");
            AssertTextDoesNotAppearInLog("secret", logger);

            // Check that the public and private arguments are passed to the child process
            var exeLogFile = DummyExeHelper.AssertDummyExeLogExists(testDir, TestContext);
            DummyExeHelper.AssertExpectedLogContents(exeLogFile, allArgs);
        }

        [TestMethod]
        public void Execute_CancellationTokenIsAlreadyCancelled_ProcessNotExecuted()
        {
            var exeName = WriteBatchFileForTest(TestContext,
                @"@echo Hello world
xxx yyy
@echo Testing 1,2,3...>&2
");

            var logger = new TestLogger();
            var args = new ProcessRunnerArguments(exeName, true);
            var runner = CreateProcessRunner(logger);

            args.CancellationToken = new CancellationToken(true);
            var output = "";
            args.HandleOutputStream = reader => output = reader.ReadToEnd();
            // Act
            runner.Execute(args);

            // Assert
            runner.ExitCode.Should().Be(0, "Unexpected exit code");

            output.Should().NotContain("Hello world");
        }

        [TestMethod]
        public void Execute_CancellationTokenCancelledMidway_CancelledDuringWritingRequest_ProcessKilled()
        {
            var exeName = WriteBatchFileForTest(TestContext, @"
echo started!
:again
   set /p arg= 
   echo %arg%
   if %arg% == END (goto finished)
   goto again
:finished
   echo done!");

            using var processCancellationTokenSource = new CancellationTokenSource();
            var logger = new TestLogger(true, true);
            var args = new ProcessRunnerArguments(exeName, true)
            {
                CancellationToken = processCancellationTokenSource.Token
            };
            var output = "";
            args.HandleOutputStream = reader => output = reader.ReadToEnd();
            args.HandleInputStream = writer =>
            {
                writer.WriteLine("dummy");
                Thread.Sleep(2500);
                writer.WriteLine("END");
            };

            var runner = CreateProcessRunner(logger);
            var processTask = Task.Run(() => { runner.Execute(args); });

            processCancellationTokenSource.CancelAfter(500);

            Task.WaitAll(new[] { processTask }, TimeSpan.FromSeconds(15));

            runner.ExitCode.Should().Be(-1, "Unexpected exit code");
            processCancellationTokenSource.IsCancellationRequested.Should().BeTrue();
            output.Should().Contain("started!");
            output.Should().Contain("dummy");
            output.Should().NotContain("done!");
        }

        [TestMethod]
        public void Execute_CancellationTokenCancelledAfterProcessAlreadyFinished_DoesNotThrow()
        {
            var exeName = WriteBatchFileForTest(TestContext,
                @"@echo Hello world
xxx yyy
@echo Testing 1,2,3...>&2
");

            var logger = new TestLogger();
            var args = new ProcessRunnerArguments(exeName, true);
            var runner = CreateProcessRunner(logger);

            var cancellationTokenSource = new CancellationTokenSource();
            args.CancellationToken = cancellationTokenSource.Token;

            runner.Execute(args);

            Action act = () => cancellationTokenSource.Cancel(false);

            act.Should().NotThrow();
        }

        #region Private methods

        private static ProcessRunner CreateProcessRunner(ILogger logger)
        {
            return new ProcessRunner(new ConfigurableSonarLintSettings(), logger);
        }

        private static void SafeSetEnvironmentVariable(string key, string value, EnvironmentVariableTarget target, ILogger logger)
        {
            try
            {
                Environment.SetEnvironmentVariable(key, value, target);
            }
            catch (System.Security.SecurityException)
            {
                logger.WriteLine("TEST SETUP ERROR: user running the test doesn't have the permissions to set the environment variable. Key: {0}, value: {1}, target: {2}",
                    key, value, target);
            }
        }

        private static void AssertTextDoesNotAppearInLog(string text, TestLogger logger)
        {
            var matches = logger.OutputStrings.Where(m => m.Contains(text));
            matches.Should().BeEmpty($"Not expecting the text to appear in the log output: {text}");
        }

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

        #endregion Private methods
    }

    internal static class LoggerExtensions
    {
        public static void AssertSingleErrorExists(this TestLogger logger, string expected)
        {
            AssertSinglePartialMessageExists(logger, CFamilyStrings.MSG_Prefix_ERROR, expected);
        }

        public static void AssertSingleDebugMessageExists(this TestLogger logger, params string[] expected)
        {
            var messageParts = new List<string>(expected);
            messageParts.Add(CFamilyStrings.MSG_Prefix_DEBUG);

            AssertSinglePartialMessageExists(logger, messageParts.ToArray());
        }

        private static void AssertSinglePartialMessageExists(this TestLogger logger, params string[] expected)
        {
            var matches = logger.OutputStrings.Where(m => expected.All(e => m.Contains(e)));
            matches.Should().ContainSingle("More than one message contains the expected strings: {0}", string.Join(",", expected));
        }
    }
}
