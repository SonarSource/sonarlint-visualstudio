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

using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class UnboundProjectFinderTests
    {
        [TestMethod]
        public void GetUnboundProjects_SolutionBound_EmptyFilteredProjects()
        {
            // Arrange - no projects created
            var testConfig = new TestConfigurationBuilder();

            var testSubject = testConfig.CreateTestSubject();

            // Act
            var result = testSubject.GetUnboundProjects();

            // Assert
            AssertEmptyResult(result);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        public void GetUnboundProjects_ValidSolution_ReturnsProjectsThatRequireBinding(int numberOfUnboundProjects)
        {
            var testConfig = new TestConfigurationBuilder();
            var project1 = testConfig.AddFilteredProject(ProjectSystemHelper.CSharpProjectKind);
            var project2 = testConfig.AddFilteredProject(ProjectSystemHelper.VbCoreProjectKind);
            var project3 = testConfig.AddFilteredProject(ProjectSystemHelper.CppProjectKind);

            var allProjects = new List<Project> {project1, project2, project3};
            var unboundProjects = allProjects.Take(numberOfUnboundProjects).ToList();
            var boundProjects = allProjects.Except(unboundProjects).ToList();

            foreach (var boundProject in boundProjects)
            {
                testConfig.SetupProjectBindingRequired(boundProject, true);
            }
            foreach (var boundProject in unboundProjects)
            {
                testConfig.SetupProjectBindingRequired(boundProject, false);
            }

            var testSubject = testConfig.CreateTestSubject();

            var result = testSubject.GetUnboundProjects();
            result.Should().AllBeEquivalentTo(unboundProjects);
        }

        private static void AssertEmptyResult(IEnumerable<EnvDTE.Project> projects)
        {
            projects.Should().NotBeNull("Null are not expected");
            projects.Should().BeEmpty("Not expecting any results. Actual: {0}", GetString(projects));
        }

        private static string GetString(IEnumerable<EnvDTE.Project> projects)
        {
            return string.Join(", ", projects.Select(p => p.FullName));
        }

        /// <summary>
        /// Builder that provides more declarative methods to set up the test environment 
        /// </summary>
        private class TestConfigurationBuilder
        {
            private readonly Mock<IProjectBinderFactory> projectBinderFactoryMock = new Mock<IProjectBinderFactory>();
            private readonly List<ProjectMock> projects = new List<ProjectMock>();
            private readonly BindingConfiguration bindingConfiguration = BindingConfiguration.Standalone;

            public ProjectMock AddFilteredProject(string projectKind)
            {
                var project = new ProjectMock("any.proj");
                project.ProjectKind = projectKind;
                projects.Add(project);
                return project;
            }

            public void SetupProjectBindingRequired(EnvDTE.Project project, bool isBindingRequired)
            {
                var projectBinderMock = new Mock<IProjectBinder>();
                projectBinderMock
                    .Setup(x => x.IsBindingRequired(bindingConfiguration, project))
                    .Returns(isBindingRequired);

                projectBinderFactoryMock
                    .Setup(x => x.Get(project))
                    .Returns(projectBinderMock.Object);
            }

            public UnboundProjectFinder CreateTestSubject()
            {
                var projectSystemHelper = new Mock<IProjectSystemHelper>();
                projectSystemHelper.Setup(x => x.GetFilteredSolutionProjects()).Returns(projects);

                var configProviderMock = new Mock<IConfigurationProviderService>();
                configProviderMock.Setup(x => x.GetConfiguration()).Returns(bindingConfiguration);

                var sp = new ConfigurableServiceProvider();
                sp.RegisterService(typeof(IProjectSystemHelper), projectSystemHelper.Object);
                sp.RegisterService(typeof(IConfigurationProviderService), configProviderMock.Object);

                var testSubject = new UnboundProjectFinder(sp, projectBinderFactoryMock.Object);
                return testSubject;
            }
        }
    }
}
