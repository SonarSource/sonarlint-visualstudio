/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Windows.Threading;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ProjectPropertyManagerTests
    {
        #region Test boilerplate

        private const string TestPropertyName = "MyTestProperty";

        private ConfigurableVsProjectSystemHelper projectSystem;
        private IHost host;

        [TestInitialize]
        public void TestInitialize()
        {
            var provider = new ConfigurableServiceProvider();
            this.projectSystem = new ConfigurableVsProjectSystemHelper(provider);

            provider.RegisterService(typeof(IProjectSystemHelper), projectSystem);
            this.host = new ConfigurableHost(provider, Dispatcher.CurrentDispatcher);
            var propertyManager = new ProjectPropertyManager(host);
            var mefModel = MefTestHelpers.CreateExport<IProjectPropertyManager>(propertyManager);

            provider.RegisterService(typeof(SComponentModel), mefModel);
        }

        #endregion Test boilerplate

        #region Tests

        [TestMethod]
        public void ProjectPropertyManager_Ctor_NullArgChecks()
        {
            // Test case 1: missing IHost (MEF failure) throws exception
            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => new ProjectPropertyManager((IHost)null));

            // Test case 2: missing IHost's local services does not fail, only asserts
            // Arrange
            var emptyHost = new ConfigurableHost(new ConfigurableServiceProvider(false), Dispatcher.CurrentDispatcher);
            using (new AssertIgnoreScope())
            {
                // Act + Assert
                new ProjectPropertyManager(emptyHost);
            }
        }

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
            this.projectSystem.SelectedProjects = expectedProjects;

            ProjectPropertyManager testSubject = this.CreateTestSubject();

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

            ProjectPropertyManager testSubject = this.CreateTestSubject();

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

            ProjectPropertyManager testSubject = this.CreateTestSubject();

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

        #endregion Tests

        #region Test helpers

        private ProjectPropertyManager CreateTestSubject()
        {
            return new ProjectPropertyManager(this.host);
        }

        #endregion Test helpers
    }
}