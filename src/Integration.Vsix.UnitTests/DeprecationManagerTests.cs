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
using FluentAssertions;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.InfoBar;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class DeprecationManagerTests
    {
        [TestMethod]
        public void Ctor_WhenInfoBarManagerIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            Action act = () => new DeprecationManager(null, new Mock<ILogger>().Object);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("infoBarManager");
        }

        [TestMethod]
        public void Ctor_WhenSonarLintOutputIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            Action act = () => new DeprecationManager(new Mock<IInfoBarManager>().Object, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sonarLintOutput");
        }

        [TestMethod]
        public void Initialize_WhenVersionIsVS2015RTM_ShowsWarnings()
        {
            AssertShowsWarnings("14.0.23107.0", Times.Once());
        }

        [TestMethod]
        public void Initialize_WhenVersionIsVS2015Update1_ShowsWarnings()
        {
            AssertShowsWarnings("14.0.24720.00", Times.Once());
        }

        [TestMethod]
        public void Initialize_WhenVersionIsVS2015Update2_ShowsWarnings()
        {
            AssertShowsWarnings("14.0.25123.00", Times.Once());
        }

        [TestMethod]
        public void Initialize_WhenVersionIsVS2015Update3_DoesNotShowWarnings()
        {
            AssertShowsWarnings("14.0.25420.00", Times.Never());
        }

        [TestMethod]
        public void Dispose__WhenInitialized_CallsCloseOnTheBar()
        {
            // Arrange
            VisualStudioHelpers.VisualStudioVersion = "0.0.0.0";

            var sonarLintOutputMock = new Mock<ILogger>();
            var inforBarManagerMock = new Mock<IInfoBarManager>();
            var infoBar = new Mock<IInfoBar>();
            inforBarManagerMock
                .Setup(x => x.AttachInfoBar(DeprecationManager.DeprecationBarGuid, It.IsAny<string>(), It.IsAny<ImageMoniker>()))
                .Returns(infoBar.Object);

            var deprecationManager = new DeprecationManager(inforBarManagerMock.Object, sonarLintOutputMock.Object);

            // Act
            deprecationManager.Dispose();

            // Assert
            infoBar.Verify(x => x.Close(), Times.Once);
        }

        [TestMethod]
        public void Dispose_WhenCalledMultipleTimes_CallsCloseOnTheBarOnlyOnce()
        {
            // Arrange
            VisualStudioHelpers.VisualStudioVersion = "0.0.0.0";

            var sonarLintOutputMock = new Mock<ILogger>();
            var inforBarManagerMock = new Mock<IInfoBarManager>();
            var infoBar = new Mock<IInfoBar>();
            inforBarManagerMock
                .Setup(x => x.AttachInfoBar(DeprecationManager.DeprecationBarGuid, It.IsAny<string>(), It.IsAny<ImageMoniker>()))
                .Returns(infoBar.Object);

            var deprecationManager = new DeprecationManager(inforBarManagerMock.Object, sonarLintOutputMock.Object);

            // Act
            deprecationManager.Dispose();
            deprecationManager.Dispose();
            deprecationManager.Dispose();

            // Assert
            infoBar.Verify(x => x.Close(), Times.Once);
        }

        private void AssertShowsWarnings(string version, Times numberOfTimes)
        {
            // Arrange
            VisualStudioHelpers.VisualStudioVersion = version;
            var sonarLintOutputMock = new Mock<ILogger>();
            var inforBarManagerMock = new Mock<IInfoBarManager>();

            string expectedOutputMessage =
                "*****************************************************************************************\r\n" +
                "***   SonarLint for Visual Studio versions 4.0+ will no longer support this version   ***\r\n" +
                "***         of Visual Studio. Please update to Visual Studio 2015 Update 3 or         ***\r\n" +
                "***               Visual Studio 2017 to benefit from new features.                    ***\r\n" +
                "*****************************************************************************************";
            string expectedBarMessage = "SonarLint for Visual Studio versions 4.0+ will no longer support this version of Visual " +
                "Studio. Please update to Visual Studio 2015 Update 3 or Visual Studio 2017 to benefit from new features.";
            // Act
            var deprecationManager = new DeprecationManager(inforBarManagerMock.Object, sonarLintOutputMock.Object);

            // Assert
            sonarLintOutputMock.Verify(x => x.WriteLine(expectedOutputMessage), numberOfTimes);
            inforBarManagerMock.Verify(x => x.AttachInfoBar(DeprecationManager.DeprecationBarGuid, expectedBarMessage,
                It.IsAny<ImageMoniker>()), numberOfTimes);
        }
    }
}
