﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Commands
{
    [TestClass]
    public class ProjectSonarLintMenuCommandTests
    {
        #region Tests

        [TestMethod]
        public void ProjectSonarLintMenuCommand_Ctor_InvalidArgs_Throws()
        {
            // Arrange
            Action act = () => new ProjectSonarLintMenuCommand(null, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("propertyManager");

            act = () => new ProjectSonarLintMenuCommand(Mock.Of<IProjectPropertyManager>(), null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("projectToLanguageMapper");
        }

        [TestMethod]
        public void ProjectSonarLintMenuCommand_QueryStatus_NoProjects_IsDisableIsHidden()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = CreateTestSubject();

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeFalse("Expected command to be disabled");
            command.Visible.Should().BeFalse("Expected command to be hidden");
        }

        [TestMethod]
        public void ProjectSonarLintMenuCommand_QueryStatus_HasUnsupportedProject_IsDisabledIsHidden()
        {
            // Arrange
            var projectToLanguageMapper = new Mock<IProjectToLanguageMapper>();
            var p1 = new ProjectMock("cs.proj");
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(p1)).Returns(true);
            var p2 = new ProjectMock("cpp.proj");
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(p2)).Returns(false);

            var projectSystem = new ConfigurableVsProjectSystemHelper(new ConfigurableServiceProvider())
            {
                SelectedProjects = new[] { p1, p2 }
            };

            var propertyManager = new Mock<IProjectPropertyManager>();
            propertyManager.Setup(x => x.GetSelectedProjects()).Returns(projectSystem.GetSelectedProjects());

            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = CreateTestSubject(propertyManager.Object, projectToLanguageMapper.Object);

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeFalse("Expected command to be disabled");
            command.Visible.Should().BeFalse("Expected command to be hidden");
        }

        [TestMethod]
        public void ProjectSonarLintMenuCommand_QueryStatus_AllSupportedProjects_IsEnabledIsVisible()
        {
            // Arrange
            var projectToLanguageMapper = new Mock<IProjectToLanguageMapper>();
            var p1 = new ProjectMock("cs1.proj");
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(p1)).Returns(true);

            var p2 = new ProjectMock("cs2.proj");
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(p2)).Returns(true);

            var projectSystem = new ConfigurableVsProjectSystemHelper(new ConfigurableServiceProvider())
            {
                SelectedProjects = new[] { p1, p2 }
            };

            var propertyManager = new Mock<IProjectPropertyManager>();
            propertyManager.Setup(x => x.GetSelectedProjects()).Returns(projectSystem.GetSelectedProjects());

            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = CreateTestSubject(propertyManager.Object, projectToLanguageMapper.Object);

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeTrue("Expected command to be enabled");
            command.Visible.Should().BeTrue("Expected command to be visible");
        }

        #endregion Tests

        private ProjectSonarLintMenuCommand CreateTestSubject(IProjectPropertyManager propertyManager = null, IProjectToLanguageMapper projectToLanguageMapper = null)
        {
            propertyManager ??= Mock.Of<IProjectPropertyManager>();
            projectToLanguageMapper ??= Mock.Of<IProjectToLanguageMapper>();

            return new ProjectSonarLintMenuCommand(propertyManager, projectToLanguageMapper);
        }
    }
}
