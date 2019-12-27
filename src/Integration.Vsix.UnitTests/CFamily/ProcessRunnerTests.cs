/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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

// Note: copied from the S4MSB
// https://github.com/SonarSource/sonar-scanner-msbuild/blob/5c23a7da9171e90a1970a31507dce3da3e8ee094/Tests/SonarScanner.MSBuild.Common.UnitTests/ProcessRunnerTests.cs#L32

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests
{
    [TestClass]
    public class ProcessRunnerTests
    {
        public TestContext TestContext { get; set; }

        #region Tests

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
            var success = runner.Execute(args);

            // Assert
            success.Should().BeFalse("Expecting the process to have failed");
            runner.ExitCode.Should().Be(-2, "Unexpected exit code");
        }

        [TestMethod]
        public void ProcRunner_ExecutionSucceeded()
        {
            // Arrange
            var exeName = WriteBatchFileForTest(TestContext,
@"@echo Hello world
xxx yyy
@echo Testing 1,2,3...>&2
");

            var logger = new TestLogger();
            var args = new ProcessRunnerArguments(exeName, true);
            var runner = CreateProcessRunner(logger);

            // Act
            var success = runner.Execute(args);

            // Assert
            success.Should().BeTrue("Expecting the process to have succeeded");
            runner.ExitCode.Should().Be(0, "Unexpected exit code");

            logger.AssertMessageLogged("Hello world"); // Check output message are passed to the logger
            logger.AssertErrorLogged("Testing 1,2,3..."); // Check error messages are passed to the logger
        }

        [TestMethod]
        public void ProcRunner_FailsOnTimeout()
        {
            // Arrange

            // Calling TIMEOUT can fail on some OSes (e.g. Windows 7) with the error
            // "Input redirection is not supported, exiting the process immediately."
            // Alternatives such as
            // pinging a non-existent address with a timeout were not reliable.
            var exeName = WriteBatchFileForTest(TestContext,
@"waitfor /t 2 somethingThatNeverHappen
@echo Hello world
");

            var logger = new TestLogger();
            var args = new ProcessRunnerArguments(exeName, true)
            {
                TimeoutInMilliseconds = 100
            };
            var runner = CreateProcessRunner(logger);

            var timer = Stopwatch.StartNew();

            // Act
            var success = runner.Execute(args);

            // Assert
            timer.Stop(); // Sanity check that the process actually timed out
            logger.WriteLine("Test output: test ran for {0}ms", timer.ElapsedMilliseconds);
            // TODO: the following line throws regularly on the CI machines (elapsed time is around 97ms)
            // timer.ElapsedMilliseconds >= 100.Should().BeTrue("Test error: batch process exited too early. Elapsed time(ms): {0}", timer.ElapsedMilliseconds)

            success.Should().BeFalse("Expecting the process to have failed");
            runner.ExitCode.Should().Be(ProcessRunner.ErrorCode, "Unexpected exit code");
            logger.AssertMessageNotLogged("Hello world");
            // expecting a warning about the timeout
            logger.AssertPartialOutputStringExists("has been terminated");
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

            // Act
            var success = runner.Execute(args);

            // Assert
            success.Should().BeTrue("Expecting the process to have succeeded");
            runner.ExitCode.Should().Be(0, "Unexpected exit code");

            logger.AssertMessageLogged("PROCESS_VAR value");
            logger.AssertMessageLogged("PROCESS_VAR2 value");
            logger.AssertMessageLogged("PROCESS_VAR3 value");
        }

        [TestMethod]
        public void ProcRunner_PassesEnvVariables_OverrideExisting()
        {
            // Tests that existing environment variables will be overwritten successfully

            // Arrange
            var logger = new TestLogger();
            var runner = CreateProcessRunner(logger);

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
                    EnvironmentVariables = envVariables
                };

                // Act
                var success = runner.Execute(args);

                // Assert
                success.Should().BeTrue("Expecting the process to have succeeded");
                runner.ExitCode.Should().Be(0, "Unexpected exit code");
            }
            finally
            {
                SafeSetEnvironmentVariable("proc.runner.test.machine", null, EnvironmentVariableTarget.Machine, logger);
                Environment.SetEnvironmentVariable("proc.runner.test.process", null, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("proc.runner.test.user", null, EnvironmentVariableTarget.User);
            }

            // Check the child process used expected values
            logger.AssertMessageLogged("file: machine override");
            logger.AssertMessageLogged("file: process override");
            logger.AssertMessageLogged("file: user override");

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
            var success = runner.Execute(args);

            // Assert
            success.Should().BeFalse("Expecting the process to have failed");
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
            var success = runner.Execute(args);

            // Assert
            success.Should().BeTrue("Expecting the process to have succeeded");
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
            var success = runner.Execute(args);

            // Assert
            success.Should().BeTrue("Expecting the process to have succeeded");
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
            var success = runner.Execute(runnerArgs);

            // Assert
            success.Should().BeTrue("Expecting the process to have succeeded");
            runner.ExitCode.Should().Be(0, "Unexpected exit code");

            // Check public arguments are logged but private ones are not
            foreach(var arg in publicArgs)
            {
                logger.AssertSingleDebugMessageExists(arg);
            }

            logger.AssertSingleDebugMessageExists("<sensitive data removed>");
            AssertTextDoesNotAppearInLog("secret", logger);

            // Check that the public and private arguments are passed to the child process
            var exeLogFile = DummyExeHelper.AssertDummyExeLogExists(testDir, TestContext);
            DummyExeHelper.AssertExpectedLogContents(exeLogFile, allArgs);
        }

        #endregion Tests

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

        private static void AssertTextDoesNotAppearInLog(string text, IList<string> logEntries)
        {
            logEntries.Should().NotContain(e => e.IndexOf(text, StringComparison.OrdinalIgnoreCase) > -1,
                "Specified text should not appear anywhere in the log file: {0}", text);
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
            Directory.CreateDirectory(testPath);
            return testPath;
        }

        #endregion Private methods
    }

    // This test class was copied from the Scanner for MSBuild repo. The test logger in the scanner has different
    // methods. To keep the bulk of this source file as similar as possible to the scanner version, we have an
    // extension class to provide test logger assertion methods with the expected names.
    internal static class LoggerExtensions
    {
        public static void AssertMessageLogged(this TestLogger logger, string expected)
        {
            logger.AssertOutputStringExists(expected);
        }

        public static void AssertErrorLogged(this TestLogger logger, string expected)
        {
            logger.AssertOutputStringExists(CFamilyStrings.MSG_Prefix_ERROR + expected);
        }

        public static void AssertMessageNotLogged(this TestLogger logger, string expected)
        {
            logger.AssertOutputStringDoesNotExist(expected);
        }

        public static void AssertWarningLogged(this TestLogger logger, string expected)
        {
            logger.AssertOutputStringExists(CFamilyStrings.MSG_Prefix_WARN + expected);
        }

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
