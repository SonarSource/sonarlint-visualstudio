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

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.TestInfrastructure;
using IVsHierarchy = Microsoft.VisualStudio.Shell.Interop.IVsHierarchy;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ProjectSystemHelperTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private SolutionMock solutionMock;
        private ProjectSystemHelper testSubject;
        private Mock<IProjectToLanguageMapper> projectToLanguageMapper;

        [TestInitialize]
        public void TestInitialize()
        {
            this.solutionMock = new SolutionMock();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.serviceProvider.RegisterService(typeof(SVsSolution), this.solutionMock);
            projectToLanguageMapper = new Mock<IProjectToLanguageMapper>();

            this.testSubject = new ProjectSystemHelper(this.serviceProvider, projectToLanguageMapper.Object);
        }

        [TestMethod]
        public void MefCtor_CheckExports()
        {
            var batch = new CompositionBatch();

            batch.AddExport(MefTestHelpers.CreateExport<SVsServiceProvider>(Mock.Of<IServiceProvider>()));
            batch.AddExport(MefTestHelpers.CreateExport<IProjectToLanguageMapper>(Mock.Of<IProjectToLanguageMapper>()));

            var helperImport = new SingleObjectImporter<IProjectSystemHelper>();
            var vsHierarchyLocator = new SingleObjectImporter<IVsHierarchyLocator>();
            batch.AddPart(helperImport);
            batch.AddPart(vsHierarchyLocator);

            var catalog = new TypeCatalog(typeof(ProjectSystemHelper));
            using var container = new CompositionContainer(catalog);
            container.Compose(batch);

            helperImport.Import.Should().NotBeNull();
            vsHierarchyLocator.Import.Should().NotBeNull();

            helperImport.Import.Should().BeSameAs(vsHierarchyLocator.Import);
        }

        #region Tests

        [TestMethod]
        public void ProjectSystemHelper_GetIVsHierarchy_ArgChecks()
        {
            // Arrange
            Action act = () => this.testSubject.GetIVsHierarchy(null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("dteProject");
        }

        [TestMethod]
        public void ProjectSystemHelper_GetIVsHierarchy()
        {
            // Arrange
            const string projectName = "project";

            // Sanity
            this.testSubject.GetIVsHierarchy(new ProjectMock(projectName)).Should().BeNull("Project not associated with the solution");

            ProjectMock project = this.solutionMock.AddOrGetProject(projectName);

            // Act
            IVsHierarchy h = this.testSubject.GetIVsHierarchy(project);

            // Assert
            h.Should().Be(project, "The test implementation of a ProjectMock is also the one for its IVsHierarcy");
        }

        [TestMethod]
        public void ProjectSystemHelper_GetSolutionProjects_ReturnsOnlyKnownLanguages()
        {
            // Arrange
            ProjectMock csProject = this.solutionMock.AddOrGetProject("c#");
            csProject.SetExtObjProperty(VSConstants.VSITEMID_ROOT, csProject);
            csProject.ProjectKind = ProjectSystemHelper.CSharpProjectKind;
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(csProject)).Returns(true);

            ProjectMock vbProject = this.solutionMock.AddOrGetProject("vb.net");
            vbProject.SetExtObjProperty(VSConstants.VSITEMID_ROOT, vbProject);
            vbProject.ProjectKind = ProjectSystemHelper.VbProjectKind;
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(vbProject)).Returns(true);

            ProjectMock otherProject = this.solutionMock.AddOrGetProject("other");
            otherProject.SetExtObjProperty(VSConstants.VSITEMID_ROOT, otherProject);
            otherProject.ProjectKind ="other";
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(otherProject)).Returns(false);

            ProjectMock erronousProject = this.solutionMock.AddOrGetProject("err");
            erronousProject.SetExtObjProperty(VSConstants.VSITEMID_ROOT, null);
            erronousProject.ProjectKind = ProjectSystemHelper.VbProjectKind;
            projectToLanguageMapper.Setup(x => x.HasSupportedLanguage(erronousProject)).Returns(true);

            // Act
            var actual = this.testSubject.GetSolutionProjects().ToArray();

            // Assert
            CollectionAssert.AreEqual(new[] { csProject, vbProject }, actual,
                "Unexpected projects: {0}", string.Join(", ", actual.Select(p => p.Name)));
        }

        [TestMethod]
        public void ProjectSystemHelper_GetSelectedProjects_ReturnsActiveProjects()
        {
            // Arrange
            var dte = new DTEMock();
            this.serviceProvider.RegisterService(typeof(SDTE), dte);

            var p1 = new ProjectMock("p1.proj");
            var p2 = new ProjectMock("p1.proj");

            var expectedProjects = new Project[] { p1, p2 };
            dte.ActiveSolutionProjects = expectedProjects;

            // Act
            Project[] actualProjects = testSubject.GetSelectedProjects().ToArray();

            // Assert
            CollectionAssert.AreEquivalent(expectedProjects, actualProjects, "Unexpected projects");
        }

        [TestMethod]
        public void ProjectSystemHelper_GetProjectProperty_ProjectArgCheck()
        {
            Action act = () => testSubject.GetProjectProperty(null, "prop");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("dteProject");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("\t\n")]
        public void ProjectSystemHelper_GetProjectProperty_PropertyNameArgCheck(string propertyName)
        {
            Action act = () => testSubject.GetProjectProperty(new ProjectMock("a.proj"), propertyName);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("propertyName");
        }

        [TestMethod]
        public void ProjectSystemHelper_GetProjectProperty_PropertyDoesNotExist_ReturnsNull()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("my.proj");

            // Act
            var actualValue = testSubject.GetProjectProperty(project, "myprop");

            // Assert
            actualValue.Should().BeNull("Expected no property value to be returned");
        }

        [TestMethod]
        public void ProjectSystemHelper_GetProjectProperty_PropertyExists_ReturnsValue()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("my.proj");

            project.SetBuildProperty("myprop", "myval");

            // Act
            var actualValue = testSubject.GetProjectProperty(project, "myprop");

            // Assert
            actualValue.Should().Be("myval", "Unexpected property value");
        }

        [TestMethod]
        public void ProjectSystemHelper_SetProjectProperty_PropertyArgCheck()
        {
            // 1. Null project
            Action act = () => testSubject.SetProjectProperty(null, "prop", "val");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("dteProject");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void ProjectSystemHelper_SetProjectProperty_PropertyArgCheck(string propertyName)
        {
            Action act = () => testSubject.SetProjectProperty(new ProjectMock("a.proj"), propertyName, "val");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("propertyName");
        }

        [TestMethod]
        public void ProjectSystemHelper_SetProjectProperty_PropertyDoesNotExist_AddsPropertyWithValue()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("my.proj");

            // Act
            testSubject.SetProjectProperty(project, "myprop", "myval");

            // Assert
            project.GetBuildProperty("myprop").Should().Be("myval", "Unexpected property value");
        }

        [TestMethod]
        public void ProjectSystemHelper_SetProjectProperty_PropertyExists_OverwritesValue()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("my.proj");

            project.SetBuildProperty("myprop", "oldval");

            // Act
            testSubject.SetProjectProperty(project, "myprop", "newval");

            // Assert
            project.GetBuildProperty("myprop").Should().Be("newval", "Unexpected property value");
        }

        [TestMethod]
        public void ProjectSystemHelper_ClearProjectProperty_ProjectArgCheck()
        {
            Action act = () => testSubject.ClearProjectProperty(null, "prop");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("dteProject");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("\t\n")]
        public void ProjectSystemHelper_ClearProjectProperty_PropertyArgCheck(string propertyName)
        {
            Action act = () => testSubject.ClearProjectProperty(new ProjectMock("a.proj"), propertyName);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("propertyName");
        }

        [TestMethod]
        public void ProjectSystemHelper_ClearProjectProperty_PropertyExists_ClearsProperty()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("my.proj");

            project.SetBuildProperty("myprop", "val");

            // Act
            testSubject.ClearProjectProperty(project, "myprop");

            // Assert
            project.GetBuildProperty("myprop").Should().BeNull("Expected property value to be cleared");
        }

        [TestMethod]
        public void ProjectSystemHelper_GetProject_ArgChecks()
        {
            // Arrange
            Action act = () => testSubject.GetProject(null);

            // Act and assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("projectHierarchy");
        }

        [TestMethod]
        public void ProjectSystemHelper_GetAggregateProjectKinds_ArgChecks()
        {
            // Arrange
            Action act = () => this.testSubject.GetAggregateProjectKinds(null).FirstOrDefault();

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("hierarchy");
        }

        [TestMethod]
        public void ProjectSystemHelper_GetAggregateProjectKinds_NoGuids_ReturnsEmpty()
        {
            // Arrange
            var project = new LegacyProjectMock("my.project");
            project.SetAggregateProjectTypeString(string.Empty);

            // Act
            Guid[] actualGuids = this.testSubject.GetAggregateProjectKinds(project).ToArray();

            // Assert
            actualGuids.Should().BeEmpty("Expected no GUIDs returned");
        }

        [TestMethod]
        public void ProjectSystemHelper_GetAggregateProjectKinds_HasGoodAndBadGuids_ReturnsSuccessfullyParsedGuidsOnly()
        {
            // Arrange
            const string guidString = ";;;F602148F607646F88F7772CC9C49BC3F;;__BAD__;;__BADGUID__;0BA323B301614B1C80D74607B7EB7F5A;;;__FOO__;;;";
            Guid[] expectedGuids = new[]
            {
                new Guid("F602148F607646F88F7772CC9C49BC3F"),
                new Guid("0BA323B301614B1C80D74607B7EB7F5A"),
            };

            var project = new LegacyProjectMock("my.project");
            project.SetAggregateProjectTypeString(guidString);

            // Act
            Guid[] actualGuids = this.testSubject.GetAggregateProjectKinds(project).ToArray();

            // Assert
            CollectionAssert.AreEquivalent(expectedGuids, actualGuids, "Unexpected project kind GUIDs returned");
        }

        [TestMethod]
        public void GetVsHierarchyForFile_NoOpenSolution_Null()
        {
            var dte = SetupDteMock(solution: null);
            serviceProvider.RegisterService(typeof(SDTE), dte.Object);

            var result = testSubject.GetVsHierarchyForFile("some file");

            result.Should().BeNull();
            dte.Verify(x=> x.Solution, Times.Once);
        }

        [TestMethod]
        public void GetVsHierarchyForFile_ProjectItemNotFound_Null()
        {
            var solution = SetupSolution(projectItem: null);
            var dte = SetupDteMock(solution.Object);
            serviceProvider.RegisterService(typeof(SDTE), dte.Object);

            var result = testSubject.GetVsHierarchyForFile("some file");

            result.Should().BeNull();
            solution.Verify(x=> x.FindProjectItem("some file"), Times.Once);
        }

        [TestMethod]
        public void GetVsHierarchyForFile_ProjectItemHasNoContainingProject_Null()
        {
            var projectItem = SetupProjectItem(containingProject: null);
            var solution = SetupSolution(projectItem.Object);
            var dte = SetupDteMock(solution.Object);
            serviceProvider.RegisterService(typeof(SDTE), dte.Object);

            var result = testSubject.GetVsHierarchyForFile("some file");

            result.Should().BeNull();
            projectItem.Verify(x=> x.ContainingProject, Times.Once);
        }

        [TestMethod]
        public void GetVsHierarchyForFile_FailedToGetProjectHierarchy_Null()
        {
            var projectItem = SetupProjectItem(Mock.Of<Project>());
            var solution = SetupSolution(projectItem.Object);
            var dte = SetupDteMock(solution.Object);
            serviceProvider.RegisterService(typeof(SDTE), dte.Object);

            var result = testSubject.GetVsHierarchyForFile("some file");

            result.Should().BeNull();
        }

        [TestMethod]
        public void GetVsHierarchyForFile_SucceededToGetProjectHierarchy_ProjectHierarchy()
        {
            var project = solutionMock.AddOrGetProject("some project");
            var projectItem = SetupProjectItem(project);
            var solution = SetupSolution(projectItem.Object);
            var dte = SetupDteMock(solution.Object);
            serviceProvider.RegisterService(typeof(SDTE), dte.Object);

            var result = testSubject.GetVsHierarchyForFile("some file");
            result.Should().Be(project);
        }

        private static Mock<DTE2> SetupDteMock(Solution solution)
        {
            var dte = new Mock<DTE2>();
            dte.Setup(x => x.Solution).Returns(solution);
            return dte;
        }

        private static Mock<Solution> SetupSolution(ProjectItem projectItem)
        {
            var solution = new Mock<Solution>();
            solution.Setup(x => x.FindProjectItem("some file")).Returns(projectItem);
            return solution;
        }

        private static Mock<ProjectItem> SetupProjectItem(Project containingProject)
        {
            var projectItem = new Mock<ProjectItem>();
            projectItem.Setup(x => x.ContainingProject).Returns(containingProject);
            return projectItem;
        }

        #endregion Tests

        #region Helpers

        private void SetSolutionFolderName(string name)
        {
            this.serviceProvider.RegisterService(typeof(SVsShell), new TestVsShell { LoadPackageStringResult = name });
        }

        private class TestVsShell : IVsShell
        {
            public string LoadPackageStringResult
            {
                get;
                set;
            }

            #region IVsShell

            int IVsShell.AdviseBroadcastMessages(IVsBroadcastMessageEvents pSink, out uint pdwCookie)
            {
                throw new NotImplementedException();
            }

            int IVsShell.AdviseShellPropertyChanges(IVsShellPropertyEvents pSink, out uint pdwCookie)
            {
                throw new NotImplementedException();
            }

            int IVsShell.GetPackageEnum(out IEnumPackages ppenum)
            {
                throw new NotImplementedException();
            }

            int IVsShell.GetProperty(int propid, out object pvar)
            {
                throw new NotImplementedException();
            }

            int IVsShell.IsPackageInstalled(ref Guid guidPackage, out int pfInstalled)
            {
                throw new NotImplementedException();
            }

            int IVsShell.IsPackageLoaded(ref Guid guidPackage, out IVsPackage ppPackage)
            {
                throw new NotImplementedException();
            }

            int IVsShell.LoadPackage(ref Guid guidPackage, out IVsPackage ppPackage)
            {
                throw new NotImplementedException();
            }

            int IVsShell.LoadPackageString(ref Guid guidPackage, uint resid, out string pbstrOut)
            {
                throw new NotImplementedException();
            }

#if VS2022
            int IVsShell.LoadUILibrary(ref Guid guidPackage, uint dwExFlags, out IntPtr phinstOut)
            {
                throw new NotImplementedException();
            }
#else
            int IVsShell.LoadUILibrary(ref Guid guidPackage, uint dwExFlags, out uint phinstOut)
            {
                throw new NotImplementedException();
            }
#endif

            int IVsShell.SetProperty(int propid, object var)
            {
                throw new NotImplementedException();
            }

            int IVsShell.UnadviseBroadcastMessages(uint dwCookie)
            {
                throw new NotImplementedException();
            }

            int IVsShell.UnadviseShellPropertyChanges(uint dwCookie)
            {
                throw new NotImplementedException();
            }

            #endregion IVsShell
        }

        #endregion Helpers
    }
}
