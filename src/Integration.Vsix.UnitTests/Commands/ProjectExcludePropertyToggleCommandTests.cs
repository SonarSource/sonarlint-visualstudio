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
using System.Windows.Threading;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.Commands
{
    [TestClass]
    public class ProjectExcludePropertyToggleCommandTests
    {
        #region Test boilerplate

        private ConfigurableVsProjectSystemHelper projectSystem;
        private IServiceProvider serviceProvider;
        private ProjectPropertyManager propertyManager;
        private Mock<IProjectToLanguageMapper> projectToLanguageMapper;

        [TestInitialize]
        public void TestInitialize()
        {
            var provider = new ConfigurableServiceProvider();
            this.projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            provider.RegisterService(typeof(IProjectSystemHelper), this.projectSystem);

            var host = new ConfigurableHost(provider, Dispatcher.CurrentDispatcher);
            propertyManager = new ProjectPropertyManager(host);
            var mefExports = MefTestHelpers.CreateExport<IProjectPropertyManager>(propertyManager);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);
            provider.RegisterService(typeof(SComponentModel), mefModel);

            this.serviceProvider = provider;
            projectToLanguageMapper = new Mock<IProjectToLanguageMapper>();
        }

        #endregion Test boilerplate

        #region Tests

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_Ctor()
        {
            Action act = () => new ProjectExcludePropertyToggleCommand(null, null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("propertyManager");

            act = () => new ProjectExcludePropertyToggleCommand(propertyManager, null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("projectToLanguageMapper");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_Invoke_SingleProject_TogglesValue()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();
            command.Enabled = true;

            var project = new ProjectMock("projecty.xxxx");
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(project)).Returns(true);

            var testSubject = CreateTestSubject();

            this.projectSystem.SelectedProjects = new[] { project };

            // Test case 1: true --toggle--> clears property
            this.SetExcludeProperty(project, true);

            // Act
            testSubject.Invoke(command, null);

            // Assert
            this.VerifyExcludeProperty(project, null);

            // Test case 2: no property --toggle--> true
            this.SetExcludeProperty(project, null);

            // Act
            testSubject.Invoke(command, null);

            // Assert
            this.VerifyExcludeProperty(project, true);
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_Invoke_MultipleProjects_ConsistentPropValues_TogglesValues()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();
            command.Enabled = true;

            var testSubject = CreateTestSubject();

            var p1 = new ProjectMock("good1.proj");
            var p2 = new ProjectMock("good2.proj");
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(p1)).Returns(true);
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(p2)).Returns(true);

            this.projectSystem.SelectedProjects = new[] { p1, p2 };

            // Test case 1: all not set --toggle--> all true
            // Act
            testSubject.Invoke(command, null);

            // Assert
            this.VerifyExcludeProperty(p1, true);
            this.VerifyExcludeProperty(p2, true);

            // Test case 2: all true --toggle--> all not set
            // Arrange
            this.SetExcludeProperty(p1, true);
            this.SetExcludeProperty(p2, true);

            // Act
            testSubject.Invoke(command, null);

            // Assert
            this.VerifyExcludeProperty(p1, null);
            this.VerifyExcludeProperty(p2, null);
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_Invoke_MultipleProjects_MixedPropValues_SetIsExcludedTrue()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();
            command.Enabled = true;

            var testSubject = CreateTestSubject();

            var p1 = new ProjectMock("trueProj.proj");
            var p2 = new ProjectMock("nullProj.proj");
            var p3 = new ProjectMock("trueProj.proj");
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(p1)).Returns(true);
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(p2)).Returns(true);
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(p3)).Returns(true);

            this.projectSystem.SelectedProjects = new[] { p1, p2, p3 };

            this.SetExcludeProperty(p1, true);
            this.SetExcludeProperty(p2, null);
            this.SetExcludeProperty(p3, false);

            // Act
            testSubject.Invoke(command, null);

            // Assert
            this.VerifyExcludeProperty(p1, true);
            this.VerifyExcludeProperty(p2, true);
            this.VerifyExcludeProperty(p3, true);
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_MissingPropertyManager_IsDisabledIsHidden()
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
        public void ProjectExcludePropertyToggleCommand_QueryStatus_SingleProject_SupportedProject_IsEnabledIsVisible()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = CreateTestSubject();

            var project = new ProjectMock("mcproject.csproj");
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(project)).Returns(true);

            this.projectSystem.SelectedProjects = new[] { project };

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeTrue("Expected command to be enabled");
            command.Visible.Should().BeTrue("Expected command to be visible");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_SingleProject_UnsupportedProject_IsDisabledIsHidden()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = CreateTestSubject();

            var project = new ProjectMock("mcproject.csproj");
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(project)).Returns(false);

            this.projectSystem.SelectedProjects = new[] { project };

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeFalse("Expected command to be disabled");
            command.Visible.Should().BeFalse("Expected command to be hidden");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_SingleProject_CheckedStateReflectsValues()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = CreateTestSubject();

            var project = new ProjectMock("face.proj");
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(project)).Returns(true);

            this.projectSystem.SelectedProjects = new[] { project };

            // Test case 1: no property -> not checked
            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Checked.Should().BeFalse("Expected command to be unchecked");

            // Test case 1: true -> is checked
            this.SetExcludeProperty(project, true);

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Checked.Should().BeTrue("Expected command to be checked");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_MultipleProjects_ConsistentPropValues_CheckedStateReflectsValues()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = CreateTestSubject();

            var p1 = new ProjectMock("good1.proj");
            var p2 = new ProjectMock("good2.proj");
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(p1)).Returns(true);
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(p2)).Returns(true);

            this.projectSystem.SelectedProjects = new[] { p1, p2 };

            // Test case 1: no property -> not checked
            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Checked.Should().BeFalse("Expected command to be unchecked");

            // Test case 2: all true -> is checked
            this.SetExcludeProperty(p1, true);
            this.SetExcludeProperty(p2, true);

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Checked.Should().BeTrue("Expected command to be checked");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_MultipleProjects_MixedPropValues_IsUnchecked()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = CreateTestSubject();

            var p1 = new ProjectMock("good1.proj");
            var p2 = new ProjectMock("good2.proj");
            var p3 = new ProjectMock("good3.proj");
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(p1)).Returns(true);
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(p2)).Returns(true);
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(p3)).Returns(true);

            this.projectSystem.SelectedProjects = new[] { p1, p2, p3 };

            this.SetExcludeProperty(p1, true);
            this.SetExcludeProperty(p2, null);
            this.SetExcludeProperty(p3, false);

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Checked.Should().BeFalse("Expected command to be unchecked");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_MultipleProjects_AllSupportedProjects_IsEnabledIsVisible()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = CreateTestSubject();

            var p1 = new ProjectMock("good1.proj");
            var p2 = new ProjectMock("good2.proj");
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(p1)).Returns(true);
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(p2)).Returns(true);

            this.projectSystem.SelectedProjects = new [] { p1, p2 };

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeTrue("Expected command to be enabled");
            command.Visible.Should().BeTrue("Expected command to be visible");
        }

        [TestMethod]
        public void ProjectExcludePropertyToggleCommand_QueryStatus_MultipleProjects_MixedSupportedProject_IsDisabledIsHidden()
        {
            // Arrange
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var testSubject = CreateTestSubject();

            var unsupportedProject = new ProjectMock("bad.proj");
            var supportedProject = new ProjectMock("good.proj");
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(unsupportedProject)).Returns(false);
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(supportedProject)).Returns(true);

            this.projectSystem.SelectedProjects = new[] { unsupportedProject, supportedProject };

            // Act
            testSubject.QueryStatus(command, null);

            // Assert
            command.Enabled.Should().BeFalse("Expected command to be disabled");
            command.Visible.Should().BeFalse("Expected command to be hidden");
        }

        #endregion Tests

        #region Test helpers

        private void VerifyExcludeProperty(ProjectMock project, bool? expected)
        {
            bool? actual = this.GetExcludeProperty(project);
            actual.Should().Be(expected);
        }

        private bool? GetExcludeProperty(ProjectMock project)
        {
            string valueString = project.GetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey);
            bool value;
            if (bool.TryParse(valueString, out value))
            {
                return value;
            }

            return null;
        }

        private void SetExcludeProperty(ProjectMock project, bool? value)
        {
            if (value.GetValueOrDefault(false))
            {
                project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, value.Value.ToString());
            }
            else
            {
                project.ClearBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey);
            }
        }

        private ProjectExcludePropertyToggleCommand CreateTestSubject() => 
            new ProjectExcludePropertyToggleCommand(propertyManager, projectToLanguageMapper.Object);

        #endregion Test helpers
    }
}
