/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Xunit;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ProjectPropertyManagerTests
    {
        private const string TestPropertyName = "MyTestProperty";

        private ConfigurableVsProjectSystemHelper projectSystem;
        private IHost host;

        public ProjectPropertyManagerTests()
        {
            var provider = new ConfigurableServiceProvider();
            this.projectSystem = new ConfigurableVsProjectSystemHelper(provider);

            provider.RegisterService(typeof(IProjectSystemHelper), projectSystem);
            this.host = new ConfigurableHost(provider, Dispatcher.CurrentDispatcher);
            var propertyManager = new ProjectPropertyManager(host);
            var mefModel = MefTestHelpers.CreateExport<IProjectPropertyManager>(propertyManager);

            provider.RegisterService(typeof(SComponentModel), mefModel);
        }

        #region Tests

        [Fact]
        public void Ctor_WithNullHost_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => new ProjectPropertyManager(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ProjectPropertyManager_Ctor_NullArgChecks()
        {
            // Test case 2: missing IHost's local services does not fail, only asserts
            // Arrange
            var emptyHost = new ConfigurableHost(new ConfigurableServiceProvider(false), Dispatcher.CurrentDispatcher);
            using (new AssertIgnoreScope())
            {
                // Act + Assert
                new ProjectPropertyManager(emptyHost);
            }
        }

        [Fact]
        public void ProjectPropertyManager_GetSelectedProject_NoSelectedProjects_ReturnsEmpty()
        {
            // Arrange
            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act
            IEnumerable<Project> actualProjects = testSubject.GetSelectedProjects();

            // Assert
            actualProjects.Any().Should().BeFalse("Expected no projects to be returned");
        }

        [Fact]
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
            actualProjects.Should().Equal(expectedProjects, "Unexpected selected projects");
        }

        [Fact]
        public void ProjectPropertyManager_GetBooleanProperty()
        {
            // Arrange
            var project = new ProjectMock("foo.proj");

            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Test case 1: no property -> null
            // Arrange
            project.ClearBuildProperty(TestPropertyName);

            // Act + Assert
            testSubject.GetBooleanProperty(project, TestPropertyName)
                .Should().BeNull("Expected null for missing property value");

            // Test case 2: bad property -> null
            // Arrange
            project.SetBuildProperty(TestPropertyName, "NotABool");

            // Act + Assert
            testSubject.GetBooleanProperty(project, TestPropertyName)
                .Should().BeNull("Expected null for bad property value");

            // Test case 3: true property -> true
            // Arrange
            project.SetBuildProperty(TestPropertyName, true.ToString());

            // Act + Assert
            testSubject.GetBooleanProperty(project, TestPropertyName).Value
                .Should().BeTrue("Expected true for 'true' property value");

            // Test case 4: false property -> false
            // Arrange
            project.SetBuildProperty(TestPropertyName, false.ToString());

            // Act + Assert
            testSubject.GetBooleanProperty(project, TestPropertyName).Value
                .Should().BeFalse("Expected true for 'true' property value");
        }

        [Fact]
        public void ProjectPropertyManager_SetBooleanProperty()
        {
            // Arrange
            var project = new ProjectMock("foo.proj");

            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Test case 1: true -> property is set true
            // Arrange
            testSubject.SetBooleanProperty(project, TestPropertyName, true);

            // Act + Assert
            project.GetBuildProperty(TestPropertyName)
                .Should().Be(true.ToString(), "Expected property value true for property true");

            // Test case 2: false -> property is set false
            // Arrange
            testSubject.SetBooleanProperty(project, TestPropertyName, false);

            // Act + Assert
            project.GetBuildProperty(TestPropertyName)
                .Should().Be(false.ToString(), "Expected property value true for property true");

            // Test case 3: null -> property is cleared
            // Arrange
            testSubject.SetBooleanProperty(project, TestPropertyName, null);

            // Act + Assert
            project.GetBuildProperty(TestPropertyName)
                .Should().BeNull("Expected property value null for property false");
        }

        [Fact]
        public void GetBooleanProperty_WithNullProject_ThrowsArgumentNullException()
        {
            // Arrange
            var project = new ProjectMock("foo.proj");
            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act
            Action act = () => testSubject.GetBooleanProperty(null, "prop");

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GetBooleanProperty_WithNullPropertyName_ThrowsArgumentNullException()
        {
            // Arrange
            var project = new ProjectMock("foo.proj");
            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act
            Action act = () => testSubject.GetBooleanProperty(project, null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void SetBooleanProperty_WithNullProject_ThrowsArgumentNullException()
        {
            // Arrange
            var project = new ProjectMock("foo.proj");
            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act
            Action act = () => testSubject.SetBooleanProperty(null, "prop", true);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void SetBooleanProperty_WithNullPropertyName_ThrowsArgumentNullException()
        {
            // Arrange
            var project = new ProjectMock("foo.proj");
            ProjectPropertyManager testSubject = this.CreateTestSubject();

            // Act
            Action act = () => testSubject.SetBooleanProperty(project, null, true);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        #endregion

        #region Test helpers

        private ProjectPropertyManager CreateTestSubject()
        {
            return new ProjectPropertyManager(this.host);
        }

        #endregion
    }
}
