/*
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
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ProjectPropertyManagerTests
    {
        private const string TestPropertyName = "MyTestProperty";

        [TestMethod]
        public void MefCtor_CheckIsExported()
            => MefTestHelpers.CheckTypeCanBeImported<ProjectPropertyManager, IProjectPropertyManager>(
                MefTestHelpers.CreateExport<IProjectSystemHelper>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
            => MefTestHelpers.CheckIsSingletonMefComponent<ProjectPropertyManager>();

        [TestMethod]
        public void ProjectPropertyManager_GetSelectedProject_NoSelectedProjects_ReturnsEmpty()
        {
            // Arrange
            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act
            IEnumerable<Project> actualProjects = testSubject.GetSelectedProjects();

            // Assert
            actualProjects.Should().BeEmpty("Expected no projects to be returned");
        }

        [TestMethod]
        public void ProjectPropertyManager_GetSelectedProjects_HasSelectedProjects_ReturnsProjects()
        {
            // Arrange
            var p1 = new ProjectMock("p1.proj");
            var p2 = new ProjectMock("p2.proj");
            var p3 = new ProjectMock("p3.proj");
            p1.SetCSProjectKind();
            p2.SetVBProjectKind();
            // p3 is unknown kind
            var expectedProjects = new ProjectMock[] { p1, p2, p3 };
            var projectSystem = CreateProjectSystem(expectedProjects);
                
            ProjectPropertyManager testSubject = this.CreateTestSubject(projectSystem);

            // Act
            Project[] actualProjects = testSubject.GetSelectedProjects().ToArray();

            // Assert
            CollectionAssert.AreEquivalent(expectedProjects, actualProjects, "Unexpected selected projects");
        }

        [TestMethod]
        public void ProjectPropertyManager_GetBooleanProperty()
        {
            // Arrange
            var project = new ProjectMock("foo.proj");
            var projectSystem = CreateProjectSystem();

            ProjectPropertyManager testSubject = this.CreateTestSubject(projectSystem);

            // Test case 1: no property -> null
            // Arrange
            project.ClearBuildProperty(TestPropertyName);

            // Act + Assert
            testSubject.GetBooleanProperty(project, TestPropertyName).Should().BeNull("Expected null for missing property value");

            // Test case 2: bad property -> null
            // Arrange
            project.SetBuildProperty(TestPropertyName, "NotABool");

            // Act + Assert
            testSubject.GetBooleanProperty(project, TestPropertyName).Should().BeNull("Expected null for bad property value");

            // Test case 3: true property -> true
            // Arrange
            project.SetBuildProperty(TestPropertyName, true.ToString());

            // Act + Assert
            testSubject.GetBooleanProperty(project, TestPropertyName).Value.Should().BeTrue("Expected true for 'true' property value");

            // Test case 4: false property -> false
            // Arrange
            project.SetBuildProperty(TestPropertyName, false.ToString());

            // Act + Assert
            testSubject.GetBooleanProperty(project, TestPropertyName).Value.Should().BeFalse("Expected true for 'true' property value");
        }

        [TestMethod]
        public void ProjectPropertyManager_SetBooleanProperty()
        {
            // Arrange
            var project = new ProjectMock("foo.proj");
            var projectSystem = CreateProjectSystem();

            ProjectPropertyManager testSubject = this.CreateTestSubject(projectSystem);

            // Test case 1: true -> property is set true
            // Arrange
            testSubject.SetBooleanProperty(project, TestPropertyName, true);

            // Act + Assert
            project.GetBuildProperty(TestPropertyName).Should().Be(true.ToString(), "Expected property value true for property true");

            // Test case 2: false -> property is set false
            // Arrange
            testSubject.SetBooleanProperty(project, TestPropertyName, false);

            // Act + Assert
            project.GetBuildProperty(TestPropertyName).Should().Be(false.ToString(), "Expected property value true for property true");

            // Test case 3: null -> property is cleared
            // Arrange
            testSubject.SetBooleanProperty(project, TestPropertyName, null);

            // Act + Assert
            project.GetBuildProperty(TestPropertyName).Should().BeNull("Expected property value null for property false");
        }

        [TestMethod]
        public void ProjectPropertyManager_GetBooleanProperty_NullArgChecks()
        {
            // Arrange
            var project = new ProjectMock("foo.proj");
            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => testSubject.GetBooleanProperty(null, "prop"));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.GetBooleanProperty(project, null));
        }

        [TestMethod]
        public void ProjectPropertyManager_SetBooleanProperty_NullArgChecks()
        {
            // Arrange
            var project = new ProjectMock("foo.proj");
            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => testSubject.SetBooleanProperty(null, "prop", true));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.SetBooleanProperty(project, null, true));
        }

        private static ConfigurableVsProjectSystemHelper CreateProjectSystem(params ProjectMock[] selectedProjects)
        {
            var projectSystem = new ConfigurableVsProjectSystemHelper(new ConfigurableServiceProvider());
            projectSystem.SelectedProjects = selectedProjects;
            return projectSystem;
        }

        private ProjectPropertyManager CreateTestSubject(IProjectSystemHelper projectSystemHelper = null)
            => new ProjectPropertyManager(projectSystemHelper ?? Mock.Of<IProjectSystemHelper>());
    }
}
