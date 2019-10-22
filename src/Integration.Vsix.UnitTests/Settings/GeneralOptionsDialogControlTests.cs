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
using System.Windows.Input;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings
{
    [TestClass]
    public class GeneralOptionsDialogControlTests
    {
        [TestMethod]
        public void Ctor_WithNullSettings_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new GeneralOptionsDialogControl(null, new Mock<ISonarLintDaemon>().Object, new Mock<IDaemonInstaller>().Object, new Mock<ICommand>().Object, new TestLogger());

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("settings");
        }

        [TestMethod]
        public void Ctor_WithNullDaemon_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new GeneralOptionsDialogControl(new Mock<ISonarLintSettings>().Object, null, new Mock<IDaemonInstaller>().Object, new Mock<ICommand>().Object, new TestLogger());

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("daemon");
        }

        [TestMethod]
        public void Ctor_WithNullInstaller_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new GeneralOptionsDialogControl(new Mock<ISonarLintSettings>().Object, new Mock<ISonarLintDaemon>().Object, null, new Mock<ICommand>().Object, new TestLogger());

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("installer");
        }

        [TestMethod]
        public void Ctor_WithNullOpenSettingsCommand_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new GeneralOptionsDialogControl(new Mock<ISonarLintSettings>().Object, new Mock<ISonarLintDaemon>().Object, new Mock<IDaemonInstaller>().Object, null, new TestLogger());

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("openSettingsFileCommand");
        }


        [TestMethod]
        public void Ctor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new GeneralOptionsDialogControl(new Mock<ISonarLintSettings>().Object, new Mock<ISonarLintDaemon>().Object, new Mock<IDaemonInstaller>().Object, new Mock<ICommand>().Object, null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }
    }
}
