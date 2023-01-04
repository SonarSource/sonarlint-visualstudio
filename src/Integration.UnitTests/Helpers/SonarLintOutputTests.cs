/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    [TestClass]
    public class SonarLintOutputTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SonarLintOutputLogger, ILogger>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<ISonarLintSettings>());
        }

        [TestMethod]
        public void Write_OutputsToWindow()
        {
            // Arrange
            var windowMock = new ConfigurableVsOutputWindow();
            var serviceProviderMock = CreateConfiguredServiceProvider(windowMock);

            var testSubject = CreateTestSubject(serviceProviderMock);

            // Act
            testSubject.WriteLine("123");
            testSubject.WriteLine("abc");

            // Assert
            var outputPane = windowMock.AssertPaneExists(VsShellUtils.SonarLintOutputPaneGuid);
            outputPane.AssertOutputStrings("123", "abc");
        }

        [TestMethod]
        [DataRow(DaemonLogLevel.Info, false)]
        [DataRow(DaemonLogLevel.Minimal, false)]
        [DataRow(DaemonLogLevel.Verbose, true)]
        public void LogVerbose_OutputsToWindowIfLogLevelIsVerbose(DaemonLogLevel logLevel, bool shouldLogMessage)
        {
            // Arrange
            var windowMock = new ConfigurableVsOutputWindow();
            var serviceProviderMock = CreateConfiguredServiceProvider(windowMock);

            var sonarLintSettings = CreateSonarLintSettings(logLevel);

            var testSubject = CreateTestSubject(serviceProviderMock, sonarLintSettings);

            testSubject.WriteLine("create window pane");
            var outputPane = windowMock.AssertPaneExists(VsShellUtils.SonarLintOutputPaneGuid);
            outputPane.Reset();

            // Act
            testSubject.LogVerbose("123 {0} {1}", "param 1", 2);
            testSubject.LogVerbose("{0} {1} abc", 1, "param 2");

            // Assert

            if (shouldLogMessage)
            {
                outputPane.AssertOutputStrings("[DEBUG] 123 param 1 2", "[DEBUG] 1 param 2 abc");
                outputPane.AssertOutputStrings(2);
            }
            else
            {
                outputPane.AssertOutputStrings(0);
            }
        }

        [TestMethod]
        [DataRow(DaemonLogLevel.Info)]
        [DataRow(DaemonLogLevel.Minimal)]
        [DataRow(DaemonLogLevel.Verbose)]
        public void LogVerbose_PrefixIsAddedIfLogLevelIsVerbose(DaemonLogLevel logLevel)
        {
            // Arrange
            var windowMock = new ConfigurableVsOutputWindow();
            var serviceProviderMock = CreateConfiguredServiceProvider(windowMock);

            var sonarLintSettings = CreateSonarLintSettings(logLevel);

            var testSubject = CreateTestSubject(serviceProviderMock, sonarLintSettings);

            testSubject.WriteLine("create window pane");
            var outputPane = windowMock.AssertPaneExists(VsShellUtils.SonarLintOutputPaneGuid);
            outputPane.Reset();

            // Act
            testSubject.WriteLine("writeline, no params");
            testSubject.WriteLine("writeline, with params: {0}", "zzz");
            testSubject.LogVerbose("verbose");

            if (logLevel == DaemonLogLevel.Verbose)
            {
                outputPane.AssertOutputStrings(3);

                var currentThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                outputPane.AssertOutputStrings(
                    $"[ThreadId {currentThreadId}] writeline, no params",
                    $"[ThreadId {currentThreadId}] writeline, with params: zzz",
                    $"[ThreadId {currentThreadId}] [DEBUG] verbose");
            }
            else
            {
                outputPane.AssertOutputStrings(2);

                outputPane.AssertOutputStrings(
                    $"writeline, no params",
                    $"writeline, with params: zzz");
            }
        }

        private static IServiceProvider CreateConfiguredServiceProvider(IVsOutputWindow outputWindow)
        {
            var serviceProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: true);
            serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);
            return serviceProvider;
        }

        private static ISonarLintSettings CreateSonarLintSettings(DaemonLogLevel logLevel)
        {
            var sonarLintSettings = new Mock<ISonarLintSettings>();
            sonarLintSettings.Setup(x => x.DaemonLogLevel).Returns(logLevel);
            return sonarLintSettings.Object;
        }

        private static SonarLintOutputLogger CreateTestSubject(IServiceProvider serviceProvider,
            ISonarLintSettings sonarLintSettings = null)
        {
            sonarLintSettings ??= Mock.Of<ISonarLintSettings>();
            return new SonarLintOutputLogger(serviceProvider, sonarLintSettings);
        }
    }
}
