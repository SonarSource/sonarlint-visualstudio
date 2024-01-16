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

using System.Linq;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.VCProjectEngine;
using Moq;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.VcxProject
{
    [TestClass]
    public class VcxProjectTypeIndicatorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<VcxProjectTypeIndicator, IVcxProjectTypeIndicator>(
                MefTestHelpers.CreateExport<IProjectSystemHelper>());
        }

        [TestMethod]
        public void GetProjectTypes_NoProjectsInSolution_EmptyResult()
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.GetProjectTypes();

            result.Should().NotBeNull();
            result.HasAnalyzableVcxProjects.Should().BeFalse();
            result.HasNonAnalyzableVcxProjects.Should().BeFalse();
        }

        [TestMethod]
        public void GetProjectTypes_NoVcxProjectsInSolution_EmptyResult()
        {
            var project1 = new Mock<Project>();
            var project2 = new Mock<Project>();
            var testSubject = CreateTestSubject(project1.Object, project2.Object);

            var result = testSubject.GetProjectTypes();

            result.Should().NotBeNull();
            result.HasAnalyzableVcxProjects.Should().BeFalse();
            result.HasNonAnalyzableVcxProjects.Should().BeFalse();

            project1.VerifyGet(x=> x.Object, Times.Once);
            project2.VerifyGet(x => x.Object, Times.Once);
        }

        [TestMethod]
        public void GetProjectTypes_OneVcxProjectInSolution_NonAnalyzable_CorrectResult()
        {
            var project1 = new Mock<Project>();
            var project2 = CreateVcProject(isAnalyzable: false);
            var project3 = new Mock<Project>();

            var testSubject = CreateTestSubject(project1.Object, project2.Object, project3.Object);

            var result = testSubject.GetProjectTypes();

            result.Should().NotBeNull();
            result.HasAnalyzableVcxProjects.Should().BeFalse();
            result.HasNonAnalyzableVcxProjects.Should().BeTrue();

            project1.VerifyGet(x => x.Object, Times.Once);
            project2.VerifyGet(x => x.Object, Times.Once);
            project3.VerifyGet(x => x.Object, Times.Once);
        }

        [TestMethod]
        public void GetProjectTypes_OneVcxProjectInSolution_Analyzable_CorrectResult()
        {
            var project1 = new Mock<Project>();
            var project2 = CreateVcProject(isAnalyzable: true);
            var project3 = new Mock<Project>();

            var testSubject = CreateTestSubject(project1.Object, project2.Object, project3.Object);

            var result = testSubject.GetProjectTypes();

            result.Should().NotBeNull();
            result.HasAnalyzableVcxProjects.Should().BeTrue();
            result.HasNonAnalyzableVcxProjects.Should().BeFalse();

            project1.VerifyGet(x => x.Object, Times.Once);
            project2.VerifyGet(x => x.Object, Times.Once);
            project3.VerifyGet(x => x.Object, Times.Once);
        }

        [TestMethod]
        public void GetProjectTypes_MultipleVcxProjectInSolution_AnalyzableAndNonAnalyzable_CorrectResult()
        {
            var projects = new[]
            {
                new Mock<Project>(),
                CreateVcProject(isAnalyzable: true),
                CreateVcProject(isAnalyzable: false),
                new Mock<Project>(),
                CreateVcProject(isAnalyzable: false),
                CreateVcProject(isAnalyzable: true),
                new Mock<Project>()
            };

            var testSubject = CreateTestSubject(projects.Select(x=> x.Object).ToArray());

            var result = testSubject.GetProjectTypes();

            result.Should().NotBeNull();
            result.HasAnalyzableVcxProjects.Should().BeTrue();
            result.HasNonAnalyzableVcxProjects.Should().BeTrue();

            foreach (var project in projects)
            {
                project.VerifyGet(x => x.Object, Times.Once);
            }
        }

        private Mock<Project> CreateVcProject(bool isAnalyzable)
        {
            var compilerTool = isAnalyzable ? Mock.Of<VCCLCompilerTool>() : null;

            var toolsCollection = new Mock<IVCCollection>();
            toolsCollection.Setup(x => x.Item("VCCLCompilerTool")).Returns(compilerTool);

            var vcConfig = new Mock<VCConfiguration>();
            vcConfig.Setup(x => x.Tools).Returns(toolsCollection.Object);

            var vcProject = new Mock<VCProject>();
            vcProject.Setup(x => x.ActiveConfiguration).Returns(vcConfig.Object);

            var project = new Mock<Project>();
            project.Setup(x => x.Object).Returns(vcProject.Object);

            return project;
        }

        private VcxProjectTypeIndicator CreateTestSubject(params Project[] projects)
        {
            var projectSystemHelper = new Mock<IProjectSystemHelper>();
            projectSystemHelper.Setup(x => x.GetSolutionProjects()).Returns(projects);

            return new VcxProjectTypeIndicator(projectSystemHelper.Object);
        }
    }
}
