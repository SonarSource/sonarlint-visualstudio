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

            var serviceProviderMock = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: true);
            serviceProviderMock.RegisterService(typeof(SVsOutputWindow), windowMock);

            var logger = new SonarLintOutputLogger(serviceProviderMock, Mock.Of<ISonarLintSettings>());

            // Act
            logger.WriteLine("123");
            logger.WriteLine("abc");

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

            var serviceProviderMock = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: true);
            serviceProviderMock.RegisterService(typeof(SVsOutputWindow), windowMock);

            var sonarLintSettings = new Mock<ISonarLintSettings>();
            sonarLintSettings.Setup(x => x.DaemonLogLevel).Returns(logLevel);

            var logger = new SonarLintOutputLogger(serviceProviderMock, sonarLintSettings.Object);

            logger.WriteLine("create window pane");

            var outputPane = windowMock.AssertPaneExists(VsShellUtils.SonarLintOutputPaneGuid);
            outputPane.Reset();

            // Act
            logger.LogVerbose("123 {0} {1}", "param 1", 2);
            logger.LogVerbose("{0} {1} abc", 1, "param 2");

            // Assert

            if (shouldLogMessage)
            {
                outputPane.AssertOutputStrings("DEBUG: 123 param 1 2", "DEBUG: 1 param 2 abc");
                outputPane.AssertOutputStrings(2);
            }
            else
            {
                outputPane.AssertOutputStrings(0);
            }
        }
    }
}
