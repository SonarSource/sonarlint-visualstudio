/*
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

using EnvDTE;
using FluentAssertions.Common;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.Commands
{
    [TestClass]
    public class ProjectTestPropertySetCommandTests
    {
        #region Tests

        [TestMethod]
        public void Ctor_ArgsCheck()
        {
            Action act = () => new ProjectTestPropertySetCommand(null, Mock.Of<IProjectToLanguageMapper>(), true);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("propertyManager");

            act = () => new ProjectTestPropertySetCommand(Mock.Of<IProjectPropertyManager>(), null, true);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("projectToLanguageMapper");
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        [DataRow(null)]
        public void Invoke_MultipleProjects_SetsValues(bool? setPropertyValue)
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();
            command.Enabled = true;

            var p1 = Mock.Of<Project>();
            var p2 = Mock.Of<Project>();
            var p3 = Mock.Of<Project>();

            var projectToLanguageMapper = new Mock<IProjectToLanguageMapper>();
            SetupHasSupportedLanguage(projectToLanguageMapper, p1, true);
            SetupHasSupportedLanguage(projectToLanguageMapper, p2, true);
            SetupHasSupportedLanguage(projectToLanguageMapper, p3, true);

            var propertyManager = CreatePropertyManager(p1, p2, p3);

            var testSubject = CreateTestSubject(setPropertyValue, propertyManager.Object, projectToLanguageMapper.Object);

            // Act
            testSubject.Invoke(command, null);

            // Assert
            propertyManager.Verify(x => x.SetBooleanProperty(p1, "SonarQubeTestProject", setPropertyValue), Times.Once);
            propertyManager.Verify(x => x.SetBooleanProperty(p2, "SonarQubeTestProject", setPropertyValue), Times.Once);
            propertyManager.Verify(x => x.SetBooleanProperty(p3, "SonarQubeTestProject", setPropertyValue), Times.Once);
        }

        [TestMethod]
        public void QueryStatus_MissingPropertyManager_IsDisabledIsHidden()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = CreateTestSubject(null);

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeFalse("Expected command to be disabled");
            command.Visible.Should().BeFalse("Expected command to be hidden");
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void QueryStatus_SingleProject_Project_Supported_Unsupported_CorrectCommandState(bool projectSupported)
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var project = Mock.Of<Project>();

            var projectToLanguageMapper = new Mock<IProjectToLanguageMapper>();
            SetupHasSupportedLanguage(projectToLanguageMapper, project, projectSupported);

            var propertyManager = CreatePropertyManager(project);

            var testSubject = CreateTestSubject(propertyManager: propertyManager.Object, projectToLanguageMapper: projectToLanguageMapper.Object);

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().IsSameOrEqualTo(projectSupported);
            command.Visible.Should().IsSameOrEqualTo(projectSupported);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void QueryStatus_SingleProject_CheckedStateReflectsValues(bool excludeValue)
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var project = Mock.Of<Project>();

            var projectToLanguageMapper = new Mock<IProjectToLanguageMapper>();
            SetupHasSupportedLanguage(projectToLanguageMapper, project, true);

            var propertyManager = CreatePropertyManager(project);
            SetupGetBooleanProperty(propertyManager, project, excludeValue);

            var testSubject = CreateTestSubject(propertyManager: propertyManager.Object, projectToLanguageMapper: projectToLanguageMapper.Object);

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Checked.Should().IsSameOrEqualTo(excludeValue);
        }

        [TestMethod]
        [DataRow(true, true, true)]
        [DataRow(false, false, false)]
        [DataRow(null, false, false)]
        [DataRow(null, true, false)]
        public void QueryStatus_MultipleProjects_CheckedStateReflectsValues(bool? excludeValue1, bool? excludeValue2, bool expectedCheckedValue)
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var p1 = Mock.Of<Project>();
            var p2 = Mock.Of<Project>();

            var projectToLanguageMapper = new Mock<IProjectToLanguageMapper>();
            SetupHasSupportedLanguage(projectToLanguageMapper, p1, true);
            SetupHasSupportedLanguage(projectToLanguageMapper, p2, true);

            var propertyManager = CreatePropertyManager(p1, p2);
            SetupGetBooleanProperty(propertyManager, p1, excludeValue1);
            SetupGetBooleanProperty(propertyManager, p2, excludeValue2);

            var testSubject = CreateTestSubject(excludeValue1, propertyManager.Object, projectToLanguageMapper.Object);

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Checked.Should().IsSameOrEqualTo(expectedCheckedValue);
        }

        [TestMethod]
        [DataRow(true, true, true)]
        [DataRow(false, false, false)]
        [DataRow(true, false, false)]
        public void QueryStatus_MultipleProjects_AllSupported_MixedSupport_CorrectCommandState(bool supportedValue1, bool supportedValue2, bool expectedCommandState)
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var p1 = Mock.Of<Project>();
            var p2 = Mock.Of<Project>();

            var projectToLanguageMapper = new Mock<IProjectToLanguageMapper>();
            SetupHasSupportedLanguage(projectToLanguageMapper, p1, supportedValue1);
            SetupHasSupportedLanguage(projectToLanguageMapper, p2, supportedValue2);

            var propertyManager = CreatePropertyManager(p1, p2);

            var testSubject = CreateTestSubject(propertyManager:propertyManager.Object, projectToLanguageMapper:projectToLanguageMapper.Object);

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().IsSameOrEqualTo(expectedCommandState);
            command.Visible.Should().IsSameOrEqualTo(expectedCommandState);
        }

        #endregion Tests

        #region Test helpers

        private static void SetupGetBooleanProperty(Mock<IProjectPropertyManager> propertyManager, Project project, bool? value) =>
            propertyManager.Setup(x => x.GetBooleanProperty(project, "SonarQubeExclude")).Returns(value);

        private static void SetupHasSupportedLanguage(Mock<IProjectToLanguageMapper> projectToLanguageMapper, Project project, bool value) =>
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(project)).Returns(value);

        private static Mock<IProjectPropertyManager> CreatePropertyManager(params Project[] projectsToReturn)
        {
            var propertyManager = new Mock<IProjectPropertyManager>();
            propertyManager.Setup(x => x.GetSelectedProjects()).Returns(projectsToReturn);
            return propertyManager;
        }

        private ProjectTestPropertySetCommand CreateTestSubject(bool? setPropertyValue = null, IProjectPropertyManager propertyManager = null, IProjectToLanguageMapper projectToLanguageMapper = null)
        {
            propertyManager ??= Mock.Of<IProjectPropertyManager>();
            projectToLanguageMapper ??= Mock.Of<IProjectToLanguageMapper>();
            return new ProjectTestPropertySetCommand(propertyManager, projectToLanguageMapper, setPropertyValue);
        }

        #endregion Test helpers
    }
}
