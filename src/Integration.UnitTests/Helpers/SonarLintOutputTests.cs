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
        public void Ctor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarLintOutputLogger(null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SonarLintOutputLogger, ILogger>(
                MefTestHelpers.CreateExport<SVsServiceProvider>());
        }

        [TestMethod]
        public void Write_OutputsToWindow()
        {
            // Arrange
            var windowMock = new ConfigurableVsOutputWindow();
            var serviceProvider = InitializeServiceProvider(windowMock);

            SonarLintOutputLogger logger = new SonarLintOutputLogger(serviceProvider);

            // Act
            logger.WriteLine("123");
            logger.WriteLine("abc");

            // Assert
            var outputPane = windowMock.AssertPaneExists(VsShellUtils.SonarLintOutputPaneGuid);
            outputPane.AssertOutputStrings("123", "abc");
        }

        [TestMethod]
        public void LogDebug_Verbose_OutputsToWindow()
        {
            // Arrange
            var sonarLintSettings = InitializeSonarLintSettingsWithDaemonLogLevel(DaemonLogLevel.Verbose);

            var windowMock = new ConfigurableVsOutputWindow();
            var serviceProvider = InitializeServiceProvider(windowMock, sonarLintSettings);

            SonarLintOutputLogger logger = new SonarLintOutputLogger(serviceProvider);

            // Act
            logger.LogDebug("123");

            // Assert
            var outputPane = windowMock.AssertPaneExists(VsShellUtils.SonarLintOutputPaneGuid);
            outputPane.AssertOutputStrings("DEBUG: 123");
        }
/*
        [TestMethod]
        public void LogDebug_Info_DoesNotOutputToWindow()
        {
            // Arrange
            var sonarLintSettings = InitializeSonarLintSettingsWithDaemonLogLevel(DaemonLogLevel.Info);

            var windowMock = new ConfigurableVsOutputWindow();
            var serviceProvider = InitializeServiceProvider(windowMock, sonarLintSettings);

            SonarLintOutputLogger logger = new SonarLintOutputLogger(serviceProvider);

            // Act
            logger.LogDebug("123");

            // Assert
           windowMock.HasPane(VsShellUtils.SonarLintOutputPaneGuid).Should().Be(false);
        }

        [TestMethod]
        public void LogDebug_Minimal_DoesNotOutputToWindow()
        {
            // Arrange
            var sonarLintSettings = InitializeSonarLintSettingsWithDaemonLogLevel(DaemonLogLevel.Minimal);

            var windowMock = new ConfigurableVsOutputWindow();
            var serviceProvider = InitializeServiceProvider(windowMock, sonarLintSettings);

            SonarLintOutputLogger logger = new SonarLintOutputLogger(serviceProvider);

            // Act
            logger.LogDebug("123");

            // Assert
            windowMock.HasPane(VsShellUtils.SonarLintOutputPaneGuid).Should().Be(false);
        }
*/
        private ConfigurableServiceProvider InitializeServiceProvider(ConfigurableVsOutputWindow window, ISonarLintSettings sonarLintSettings = null)
        {
            sonarLintSettings ??= Mock.Of<ISonarLintSettings>();

            var serviceProviderMock = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: true);
            serviceProviderMock.RegisterService(typeof(SVsOutputWindow), window);
            serviceProviderMock.RegisterService(typeof(ISonarLintSettings), sonarLintSettings);

            return serviceProviderMock;
        }

        private ISonarLintSettings InitializeSonarLintSettingsWithDaemonLogLevel(DaemonLogLevel logLevel)
        {
            var sonarLintSettingsMock = new Mock<ISonarLintSettings>();
            sonarLintSettingsMock.Setup(x => x.DaemonLogLevel).Returns(logLevel);

            return sonarLintSettingsMock.Object;
        }
    }
}
