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
    public class ProjectExcludePropertyToggleCommandTests
    {
        #region Tests

        [TestMethod]
        public void Ctor_ArgsCheck()
        {
            Action act = () => new ProjectExcludePropertyToggleCommand(null, null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("propertyManager");

            act = () => new ProjectExcludePropertyToggleCommand(Mock.Of<IProjectPropertyManager>(), null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("projectToLanguageMapper");
        }

        [TestMethod]
        [DataRow(true, null)]
        [DataRow(false, true)]
        public void Invoke_SingleProject_TogglesValue(bool initialExcludeValue, bool? expectedSetValue)
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();
            command.Enabled = true;

            var project = Mock.Of<Project>();

            var projectToLanguageMapper = new Mock<IProjectToLanguageMapper>();
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(project)).Returns(true);

            var propertyManager = CreatePropertyManager(project);
            SetupGetBooleanProperty(propertyManager, project, initialExcludeValue);

            var testSubject = CreateTestSubject(propertyManager.Object, projectToLanguageMapper.Object);

            // Act
            testSubject.Invoke(command, null);

            // Assert
            VerifyBooleanPropertyCalls(propertyManager, project, expectedSetValue, 1);
        }

        [TestMethod]
        [DataRow(true, true, null)]   // Same across projects -> toggle
        [DataRow(false, false, true)] // Same across projects -> toggle
        [DataRow(null, null, true)]   // Same across projects (null === false) -> set to true
        [DataRow(null, false, true)]  // Same across projects (null === false) -> set to true
        [DataRow(null, true, true)]   // Different across projects -> set to true
        [DataRow(false, true, true)]  // Different across projects -> set to true
        public void Invoke_MultipleProjects_TogglesValues(bool? initialExcludeValue1, bool? initialExcludeValue2,
            bool? expectedSetValue)
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();
            command.Enabled = true;

            var p1 = Mock.Of<Project>();
            var p2 = Mock.Of<Project>();

            var projectToLanguageMapper = new Mock<IProjectToLanguageMapper>();
            SetupHasSupportedLanguage(projectToLanguageMapper, p1, true);
            SetupHasSupportedLanguage(projectToLanguageMapper, p2, true);

            var propertyManager = CreatePropertyManager(p1, p2);
            SetupGetBooleanProperty(propertyManager, p1, initialExcludeValue1);
            SetupGetBooleanProperty(propertyManager, p2, initialExcludeValue2);

            var testSubject = CreateTestSubject(propertyManager.Object, projectToLanguageMapper.Object);

            // Act
            testSubject.Invoke(command, null);

            // Assert

            // If values are the same across projects it is expected for getBoolean to be called twice.
            var expectedCallsToGetBoolean = initialExcludeValue1 == initialExcludeValue2 ? 2 : 1;

            VerifyBooleanPropertyCalls(propertyManager, p1, expectedSetValue, expectedCallsToGetBoolean);
            VerifyBooleanPropertyCalls(propertyManager, p2, expectedSetValue, expectedCallsToGetBoolean);
        }

        [TestMethod]
        public void QueryStatus_MissingPropertyManager_IsDisabledIsHidden()
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

            var testSubject = CreateTestSubject(propertyManager.Object, projectToLanguageMapper.Object);

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

            var testSubject = CreateTestSubject(propertyManager.Object, projectToLanguageMapper.Object);

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

            var testSubject = CreateTestSubject(propertyManager.Object, projectToLanguageMapper.Object);

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

            var testSubject = CreateTestSubject(propertyManager.Object, projectToLanguageMapper.Object);

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().IsSameOrEqualTo(expectedCommandState);
            command.Visible.Should().IsSameOrEqualTo(expectedCommandState);
        }

        #endregion Tests

        #region Test helpers

        private static void SetupHasSupportedLanguage(Mock<IProjectToLanguageMapper> projectToLanguageMapper, Project project, bool value) =>
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(project)).Returns(value);

        private static void VerifyBooleanPropertyCalls(Mock<IProjectPropertyManager> propertyManager, Project project, bool? value, int nrOfCallsToGetBoolean)
        {
            propertyManager.Verify(x => x.GetBooleanProperty(project, "SonarQubeExclude"), Times.Exactly(nrOfCallsToGetBoolean));
            propertyManager.Verify(x => x.SetBooleanProperty(project, "SonarQubeExclude", value), Times.Once);
        }

        private static void SetupGetBooleanProperty(Mock<IProjectPropertyManager> propertyManager, Project project, bool? value) =>
            propertyManager.Setup(x => x.GetBooleanProperty(project, "SonarQubeExclude")).Returns(value);

        private static Mock<IProjectPropertyManager> CreatePropertyManager(params Project[] projectsToReturn)
        {
            var propertyManager = new Mock<IProjectPropertyManager>();
            propertyManager.Setup(x => x.GetSelectedProjects()).Returns(projectsToReturn);
            return propertyManager;
        }

        private ProjectExcludePropertyToggleCommand CreateTestSubject(IProjectPropertyManager propertyManager = null,
            IProjectToLanguageMapper projectToLanguageMapper = null)
        {
            propertyManager ??= Mock.Of<IProjectPropertyManager>();
            projectToLanguageMapper ??= Mock.Of<IProjectToLanguageMapper>();

            return new ProjectExcludePropertyToggleCommand(propertyManager, projectToLanguageMapper);
        }

        #endregion Test helpers
    }
}
