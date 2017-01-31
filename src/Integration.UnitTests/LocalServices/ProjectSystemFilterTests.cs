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

using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using System;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Xunit;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{

    public class ProjectSystemFilterTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsProjectSystemHelper projectSystem;

        public ProjectSystemFilterTests()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            this.projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystem);

            var host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);

            var propertyManager = new ProjectPropertyManager(host);
            var mefExports = MefTestHelpers.CreateExport<IProjectPropertyManager>(propertyManager);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);
            this.serviceProvider.RegisterService(typeof(SComponentModel), mefModel);
        }

        #region Tests

        [Fact]
        public void Ctor_WithNullHost_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => new ProjectSystemFilter(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("host");
        }

        [Fact]
        public void IsAccepted_WithNullProject_ThrowsArgumentNullException()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();

            // Act
            Action act = () => testSubject.IsAccepted(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void IsAccepted_WithVsHierarchyFailure_ThrowsArgumentException()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();
            this.projectSystem.SimulateIVsHierarchyFailure = true;

            // Act
            Action act = () => testSubject.IsAccepted(new ProjectMock("harry.proj"));

            // Assert
            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void ProjectSystemFilter_IsAccepted_SupportedCSharpProject_ProjectExcludedViaProjectProperty()
        {
            ProjectSystemFilter_IsAccepted_SupportedProject_ProjectExcludedViaProjectProperty(".csproj");
        }

        [Fact]
        public void ProjectSystemFilter_IsAccepted_SupportedVBNetProject_ProjectExcludedViaProjectProperty()
        {
            ProjectSystemFilter_IsAccepted_SupportedProject_ProjectExcludedViaProjectProperty(".vbproj");
        }

        private void ProjectSystemFilter_IsAccepted_SupportedProject_ProjectExcludedViaProjectProperty(string projectExtension)
        {
            // Arrange
            var testSubject = this.CreateTestSubject();
            var project = new ProjectMock("supported" + projectExtension);
            project.SetCSProjectKind();
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, "False"); // Should not matter

            // Test case 1: missing property-> is accepted
            // Act
            var result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeTrue("Project with missing property SonarQubeExclude should be accepted");

            // Test case 2: property non-bool -> is accepted
            // Arrange
            project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, string.Empty);

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeTrue("Project with non-bool property SonarQubeExclude should be accepted");

            // Test case 3: property non-bool, non-empty -> is accepted
            // Arrange
            project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, "abc");

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeTrue("Project with non-bool property SonarQubeExclude should be accepted");

            // Test case 4: property true -> not accepted
            // Arrange
            project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, "true");

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeFalse("Project with property SonarQubeExclude=false should NOT be accepted");

            // Test case 5: property false -> is accepted
            // Arrange
            project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, "false");

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeTrue("Project with property SonarQubeExclude=true should be accepted");
        }

        [Fact]
        public void ProjectSystemFilter_IsAccepted_SupportedNotExcludedCSharpProject_TestProjectExcludedViaProjectProperty()
        {
            ProjectSystemFilter_IsAccepted_SupportedNotExcludedProject_TestProjectExcludedViaProjectProperty(".csproj");
        }

        [Fact]
        public void ProjectSystemFilter_IsAccepted_SupportedNotExcludedVBNetProject_TestProjectExcludedViaProjectProperty()
        {
            ProjectSystemFilter_IsAccepted_SupportedNotExcludedProject_TestProjectExcludedViaProjectProperty(".vbproj");
        }

        private void ProjectSystemFilter_IsAccepted_SupportedNotExcludedProject_TestProjectExcludedViaProjectProperty(string projectExtension)
        {
            // Arrange
            var testSubject = this.CreateTestSubject();

            var project = new ProjectMock("supported" + projectExtension);
            project.SetCSProjectKind();
            project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, "false"); // Should evaluate test projects even if false

            // Test case 1: missing property -> accepted
            // Act
            var result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeTrue("Project with missing property SonarQubeTestProject should be accepted");

            // Test case 2: empty -> accepted
            // Arrange
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, string.Empty);

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeTrue("Project with non-bool property SonarQubeTestProject should be accepted");

            // Test case 3: non-bool, non-empty -> not accepted
            // Arrange
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, "123");

            // Act
            result = testSubject.IsAccepted(project);

            // Test case 4: property true -> not accepted
            // Arrange
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, "true");

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeFalse("Project with property SonarQubeTestProject=false should NOT be accepted");

            // Test case 5: property false -> is accepted
            // Arrange
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, "false");

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeTrue("Project with property SonarQubeTestProject=true should be accepted");
        }

        [Fact]
        public void ProjectSystemFilter_IsAccepted_SupportedNotExcludedCSharpProject_IsKnownTestProject()
        {
            ProjectSystemFilter_IsAccepted_SupportedNotExcludedProject_IsKnownTestProject(".csproj");
        }

        [Fact]
        public void ProjectSystemFilter_IsAccepted_SupportedNotExcludedVBNetProject_IsKnownTestProject()
        {
            ProjectSystemFilter_IsAccepted_SupportedNotExcludedProject_IsKnownTestProject(".vbproj");
        }

        private void ProjectSystemFilter_IsAccepted_SupportedNotExcludedProject_IsKnownTestProject(string projectExtension)
        {
            // Arrange
            var testSubject = this.CreateTestSubject();

            var project = new ProjectMock("knownproject" + projectExtension);
            project.SetBuildProperty(Constants.SonarQubeExcludeBuildPropertyKey, "false"); // Should evaluate test projects even if false
            project.SetCSProjectKind();

            // Case 1: Test not test project kind, test project exclude not set
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, ""); // Should not continue with evaluation if has boolean value

            // Act
            bool result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeTrue("Project not a known test project");

            // Case 2: Test project kind, test project exclude not set
            project.SetTestProject();

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeFalse("Project of known test project type should NOT be accepted");

            // Case 3: SonarQubeTestProjectBuildPropertyKey == false, should take precedence over project kind condition
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, "false");
            project.ClearProjectKind();

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeTrue("Should be accepted since test project is explicitly not-excluded");

            // Case 4: SonarQubeTestProjectBuildPropertyKey == true, should take precedence over project kind condition
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, "true");
            project.ClearProjectKind();

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeFalse("Should not be accepted since test project is excluded");
        }

        [Fact]
        public void ProjectSystemFilter_IsAccepted_SupportedNotExcludedCSharpProject_NotExcludedTestProject_EvaluateRegex()
        {
            ProjectSystemFilter_IsAccepted_SupportedNotExcludedProject_NotExcludedTestProject_EvaluateRegex(".csproj");
        }

        [Fact]
        public void ProjectSystemFilter_IsAccepted_SupportedNotExcludedVBNetProject_NotExcludedTestProject_EvaluateRegex()
        {
            ProjectSystemFilter_IsAccepted_SupportedNotExcludedProject_NotExcludedTestProject_EvaluateRegex(".vbproj");
        }

        private void ProjectSystemFilter_IsAccepted_SupportedNotExcludedProject_NotExcludedTestProject_EvaluateRegex(string projectExtension)
        {
            // Arrange
            var testSubject = this.CreateTestSubject();
            var project = new ProjectMock("foobarfoobar" + projectExtension);
            project.SetCSProjectKind();

            // Case 1: Regex match
            testSubject.SetTestRegex(new Regex(".*barfoo.*", RegexOptions.None, TimeSpan.FromSeconds(1)));

            // Act
            var result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeFalse("Project with name that matches test regex should NOT be accepted");

            // Case 2: Regex doesn't match
            testSubject.SetTestRegex(new Regex(".*notfound.*", RegexOptions.None, TimeSpan.FromSeconds(1)));

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeTrue("Project with name that does not match test regex should be accepted");

            // Case 3: SonarQubeTestProjectBuildPropertyKey == false, should take precedence over regex condition
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, "false");

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeTrue("Should be accepted since test project is explicitly not-excluded");

            // Case 4: SonarQubeTestProjectBuildPropertyKey == true, should take precedence over regex condition
            project.SetBuildProperty(Constants.SonarQubeTestProjectBuildPropertyKey, "true");
            project.ClearProjectKind();

            // Act
            result = testSubject.IsAccepted(project);

            // Assert
            result.Should().BeFalse("Should not be accepted since test project is excluded");
        }

        [Fact]
        public void SetTestRegex_WithNullRegex_ThrowsArgumentNullException()
        {
            // Arrange
            ProjectSystemFilter testSubject = this.CreateTestSubject();

            // Act
            Action act = () => testSubject.SetTestRegex(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("regex");
        }

        #endregion

        #region Helpers

        private ProjectSystemFilter CreateTestSubject()
        {
            var host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            return new ProjectSystemFilter(host);
        }

        #endregion
    }
}
