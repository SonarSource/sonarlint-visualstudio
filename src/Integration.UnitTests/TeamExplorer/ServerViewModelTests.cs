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

using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using SonarLint.VisualStudio.Integration.Resources;
using System.Globalization;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    public class ServerViewModelTests
    {
        [Fact]
        public void Ctor_WithNullConnectionInfo_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => new ServerViewModel(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ServerViewModel_Ctor()
        {
            // Arrange
            var connInfo = new ConnectionInformation(new Uri("https://myawesomeserver:1234/"));
            IEnumerable<ProjectInformation> projects = new[]
            {
                new ProjectInformation { Name = "Project1", Key="1" },
                new ProjectInformation { Name = "Project2", Key="2" },
                new ProjectInformation { Name = "Project3", Key="3" },
                new ProjectInformation { Name = "Project4", Key="4" }
            };
            string[] projectKeys = projects.Select(x => x.Key).ToArray();

            // Case 0: default constructed state
            // Act
            var emptyViewModel = new ServerViewModel(connInfo);

            // Assert
            emptyViewModel.IsExpanded.Should().BeTrue();
            emptyViewModel.ShowAllProjects.Should().BeFalse();

            // Case 1, projects with default IsExpanded value
            // Act
            var viewModel = new ServerViewModel(connInfo);
            viewModel.SetProjects(projects);

            // Assert
            string[] vmProjectKeys = viewModel.Projects.Select(x => x.Key).ToArray();

            viewModel.ShowAllProjects.Should().BeTrue();
            viewModel.IsExpanded.Should().BeTrue();
            connInfo.ServerUri.Should().Be(viewModel.Url);
            vmProjectKeys.Should().Equal(projectKeys,
                $"VM projects [{string.Join(", ", vmProjectKeys)}] do not match input projects [{string.Join(", ", projectKeys)}]"
            );

            // Case 2, null projects with non default IsExpanded value
            // Act
            var viewModel2 = new ServerViewModel(connInfo, isExpanded: false);

            // Assert
            viewModel2.Projects.Count.Should().Be(0, "Not expecting projects");
            viewModel2.IsExpanded.Should().BeFalse();
        }

        [Fact]
        public void ServerViewModel_SetProjects()
        {
            // Arrange
            var connInfo = new ConnectionInformation(new Uri("https://myawesomeserver:1234/"));
            var viewModel = new ServerViewModel(connInfo);
            IEnumerable<ProjectInformation> projects = new[]
            {
                new ProjectInformation { Name = "Project3", Key="1" },
                new ProjectInformation { Name = "Project2", Key="2" },
                new ProjectInformation { Name = "project1", Key="3" },
            };
            string[] expectedOrderedProjectNames = projects.Select(p => p.Name).OrderBy(n => n, StringComparer.CurrentCulture).ToArray();

            // Act
            viewModel.SetProjects(projects);

            // Assert
            string[] actualProjectNames = viewModel.Projects.Select(p => p.ProjectInformation.Name).OrderBy(n => n, StringComparer.CurrentCulture).ToArray();
            actualProjectNames.Should().Equal(expectedOrderedProjectNames,
               $"VM projects [{string.Join(", ", actualProjectNames)}] do not match the expected projects [{string.Join(", ", expectedOrderedProjectNames)}]"
           );

            // Act again
            var newProject = new ProjectInformation();
            viewModel.SetProjects(new[] { newProject });

            // Assert that the collection was replaced with the new one
            viewModel.Projects.SingleOrDefault()?.ProjectInformation.Should().Be(newProject, "Expected a single project to be present");
        }

        [Fact]
        public void ServerViewModel_AutomationName()
        {
            // Arrange
            var connInfo = new ConnectionInformation(new Uri("https://myawesomeserver:1234/"));
            var testSubject = new ServerViewModel(connInfo);
            var projects = new[] { new ProjectInformation { Key = "P", Name = "A Project" } };

            var expectedProjects = string.Format(CultureInfo.CurrentCulture, Strings.AutomationServerDescription, connInfo.ServerUri);
            var expectedNoProjects = string.Format(CultureInfo.CurrentCulture, Strings.AutomationServerNoProjectsDescription, connInfo.ServerUri);

            // Test case 1: no projects
            // Act
            var actualNoProjects = testSubject.AutomationName;

            // Assert
            expectedNoProjects.Should().Be(actualNoProjects, "Unexpected description of SonarQube server without projects");

            // Test case 2: projects
            // Act
            testSubject.SetProjects(projects);
            var actualProjects = testSubject.AutomationName;

            // Assert
            expectedProjects.Should().Be(actualProjects, "Unexpected description of SonarQube server with projects");
        }
    }
}
