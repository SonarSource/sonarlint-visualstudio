//-----------------------------------------------------------------------
// <copyright file="ServerViewModelTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using SonarLint.VisualStudio.Integration.Resources;
using System.Globalization;

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
            // Setup
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

            // Verify
            Assert.IsTrue(emptyViewModel.IsExpanded);
            Assert.IsFalse(emptyViewModel.ShowAllProjects);

            // Case 1, projects with default IsExpanded value
            // Act
            var viewModel = new ServerViewModel(connInfo);
            viewModel.SetProjects(projects);

            // Verify
            string[] vmProjectKeys = viewModel.Projects.Select(x => x.Key).ToArray();

            Assert.IsTrue(viewModel.ShowAllProjects);
            Assert.IsTrue(viewModel.IsExpanded);
            Assert.AreEqual(connInfo.ServerUri, viewModel.Url);
            CollectionAssert.AreEqual(
                expected: projectKeys,
                actual: vmProjectKeys,
                message: $"VM projects [{string.Join(", ", vmProjectKeys)}] do not match input projects [{string.Join(", ", projectKeys)}]"
            );

            // Case 2, null projects with non default IsExpanded value
            // Act
            var viewModel2 = new ServerViewModel(connInfo, isExpanded: false);

            // Verify
            Assert.AreEqual(0, viewModel2.Projects.Count, "Not expecting projects");
            Assert.IsFalse(viewModel2.IsExpanded);
        }

        [TestMethod]
        public void ServerViewModel_SetProjects()
        {
            // Setup
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

            // Verify
            string[] actualProjectNames = viewModel.Projects.Select(p => p.ProjectInformation.Name).OrderBy(n => n, StringComparer.CurrentCulture).ToArray();
            CollectionAssert.AreEqual(
               expectedOrderedProjectNames,
               actualProjectNames,
               message: $"VM projects [{string.Join(", ", actualProjectNames)}] do not match the expected projects [{string.Join(", ", expectedOrderedProjectNames)}]"
           );

            // Act again
            var newProject = new ProjectInformation();
            viewModel.SetProjects(new[] { newProject });

            // Verify that the collection was replaced with the new one
            Assert.AreSame(newProject, viewModel.Projects.SingleOrDefault()?.ProjectInformation, "Expected a single project to be present");
        }

        [TestMethod]
        public void ServerViewModel_AutomationName()
        {
            // Setup
            var connInfo = new ConnectionInformation(new Uri("https://myawesomeserver:1234/"));
            var testSubject = new ServerViewModel(connInfo);
            var projects = new[] { new ProjectInformation { Key = "P", Name = "A Project" } };

            var expectedProjects = string.Format(CultureInfo.CurrentCulture, Strings.AutomationServerDescription, connInfo.ServerUri);
            var expectedNoProjects = string.Format(CultureInfo.CurrentCulture, Strings.AutomationServerNoProjectsDescription, connInfo.ServerUri);

            // Test case 1: no projects
            // Act
            var actualNoProjects = testSubject.AutomationName;

            // Verify
            Assert.AreEqual(expectedNoProjects, actualNoProjects, "Unexpected description of SonarQube server without projects");

            // Test case 2: projects
            // Act
            testSubject.SetProjects(projects);
            var actualProjects = testSubject.AutomationName;

            // Verify
            Assert.AreEqual(expectedProjects, actualProjects, "Unexpected description of SonarQube server with projects");
        }
    }
}
