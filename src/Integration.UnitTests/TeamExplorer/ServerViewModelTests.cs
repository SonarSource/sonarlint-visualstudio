/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Globalization;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    [TestClass]
    public class ServerViewModelTests
    {
        [TestMethod]
        public void ServerViewModel_Ctor_NullArgumentChecks()
        {
            var connInfo = new ConnectionInformation(new Uri("http://localhost"));

            Exceptions.Expect<ArgumentNullException>(() =>
            {
                new ServerViewModel(null);
            });
        }

        [TestMethod]
        public void ServerViewModel_Ctor()
        {
            // Arrange
            var connInfo = new ConnectionInformation(new Uri("https://myawesomeserver:1234/"));
            IEnumerable<SonarQubeProject> projects = new[]
            {
                new SonarQubeProject { Name = "Project1", Key="1" },
                new SonarQubeProject { Name = "Project2", Key="2" },
                new SonarQubeProject { Name = "Project3", Key="3" },
                new SonarQubeProject { Name = "Project4", Key="4" }
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
            viewModel.Url.Should().Be(connInfo.ServerUri);
            CollectionAssert.AreEqual(
                expected: projectKeys,
                actual: vmProjectKeys,
                message: $"VM projects [{string.Join(", ", vmProjectKeys)}] do not match input projects [{string.Join(", ", projectKeys)}]"
            );

            // Case 2, null projects with non default IsExpanded value
            // Act
            var viewModel2 = new ServerViewModel(connInfo, isExpanded: false);

            // Assert
            viewModel2.Projects.Should().BeEmpty("Not expecting projects");
            viewModel2.IsExpanded.Should().BeFalse();
        }

        [TestMethod]
        public void ServerViewModel_SetProjects()
        {
            // Arrange
            var connInfo = new ConnectionInformation(new Uri("https://myawesomeserver:1234/"));
            var viewModel = new ServerViewModel(connInfo);
            IEnumerable<SonarQubeProject> projects = new[]
            {
                new SonarQubeProject { Name = "Project3", Key="1" },
                new SonarQubeProject { Name = "Project2", Key="2" },
                new SonarQubeProject { Name = "project1", Key="3" },
            };
            string[] expectedOrderedProjectNames = projects.Select(p => p.Name).OrderBy(n => n, StringComparer.CurrentCulture).ToArray();

            // Act
            viewModel.SetProjects(projects);

            // Assert
            string[] actualProjectNames = viewModel.Projects.Select(p => p.SonarQubeProject.Name).OrderBy(n => n, StringComparer.CurrentCulture).ToArray();
            CollectionAssert.AreEqual(
               expectedOrderedProjectNames,
               actualProjectNames,
               message: $"VM projects [{string.Join(", ", actualProjectNames)}] do not match the expected projects [{string.Join(", ", expectedOrderedProjectNames)}]"
           );

            // Act again
            var newProject = new SonarQubeProject();
            viewModel.SetProjects(new[] { newProject });

            // Assert that the collection was replaced with the new one
            viewModel.Projects.SingleOrDefault()?.SonarQubeProject.Should().Be(newProject, "Expected a single project to be present");
        }

        [TestMethod]
        public void ServerViewModel_AutomationName()
        {
            // Arrange
            var connInfo = new ConnectionInformation(new Uri("https://myawesomeserver:1234/"));
            var testSubject = new ServerViewModel(connInfo);
            var projects = new[] { new SonarQubeProject { Key = "P", Name = "A Project" } };

            var expectedProjects = string.Format(CultureInfo.CurrentCulture, Strings.AutomationServerDescription, connInfo.ServerUri);
            var expectedNoProjects = string.Format(CultureInfo.CurrentCulture, Strings.AutomationServerNoProjectsDescription, connInfo.ServerUri);

            // Test case 1: no projects
            // Act
            var actualNoProjects = testSubject.AutomationName;

            // Assert
            actualNoProjects.Should().Be(expectedNoProjects, "Unexpected description of SonarQube server without projects");

            // Test case 2: projects
            // Act
            testSubject.SetProjects(projects);
            var actualProjects = testSubject.AutomationName;

            // Assert
            actualProjects.Should().Be(expectedProjects, "Unexpected description of SonarQube server with projects");
        }
    }
}