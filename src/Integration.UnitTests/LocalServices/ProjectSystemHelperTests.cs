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
using System.Linq;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ProjectSystemHelperTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private SolutionMock solutionMock;
        private ProjectSystemHelper testSubject;
        private ConfigurableProjectSystemFilter projectFilter;

        [TestInitialize]
        public void TestInitialize()
        {
            this.solutionMock = new SolutionMock();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.serviceProvider.RegisterService(typeof(SVsSolution), this.solutionMock);
            this.testSubject = new ProjectSystemHelper(this.serviceProvider);

            this.projectFilter = new ConfigurableProjectSystemFilter();
            this.serviceProvider.RegisterService(typeof(IProjectSystemFilter), this.projectFilter);
        }

        #region Tests

        [TestMethod]
        public void ArgCheck()
        {
            // Arrange
            Action act = () => new ProjectSystemHelper(null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void GetIVsHierarchy_ArgChecks()
        {
            // Arrange
            Action act = () => this.testSubject.GetIVsHierarchy(null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("dteProject");
        }

        [TestMethod]
        public void GetIVsHierarchy()
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
        public void GetSolutionProjects_ReturnsOnlyKnownLanguages()
        {
            // Arrange
            ProjectMock csProject = this.solutionMock.AddOrGetProject("c#");
            csProject.SetExtObjProperty(VSConstants.VSITEMID_ROOT, csProject);
            csProject.ProjectKind = ProjectSystemHelper.CSharpProjectKind;
            ProjectMock vbProject = this.solutionMock.AddOrGetProject("vb.net");
            vbProject.SetExtObjProperty(VSConstants.VSITEMID_ROOT, vbProject);
            vbProject.ProjectKind = ProjectSystemHelper.VbProjectKind;
            ProjectMock otherProject = this.solutionMock.AddOrGetProject("other");
            otherProject.SetExtObjProperty(VSConstants.VSITEMID_ROOT, otherProject);
            otherProject.ProjectKind ="other";
            ProjectMock erronousProject = this.solutionMock.AddOrGetProject("err");
            erronousProject.SetExtObjProperty(VSConstants.VSITEMID_ROOT, null);
            erronousProject.ProjectKind = ProjectSystemHelper.VbProjectKind;

            // Act
            var actual = this.testSubject.GetSolutionProjects().ToArray();

            // Assert
            CollectionAssert.AreEqual(new[] { csProject, vbProject }, actual,
                "Unexpected projects: {0}", string.Join(", ", actual.Select(p => p.Name)));
        }

        [TestMethod]
        public void IsFileInProject_ProjectArgCheck()
        {
            Action act = () => this.testSubject.IsFileInProject(null, "file");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("project");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("\t\n")]
        public void IsFileInProject_FileArgCheck(string file)
        {
            Action act = () => this.testSubject.IsFileInProject(new ProjectMock("project"), file);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("file");
        }

        [TestMethod]
        public void IsFileInProject()
        {
            // Arrange
            ProjectMock project1 = this.solutionMock.AddOrGetProject("project1");
            ProjectMock project2 = this.solutionMock.AddOrGetProject("project2");
            const string file1 = "file1";
            const string file2 = "file2";
            const string file3 = "file3";
            project1.AddOrGetFile(file1);
            project1.AddOrGetFile(file2);

            // Act + Assert
            this.testSubject.IsFileInProject(project1, file1).Should().BeTrue();
            this.testSubject.IsFileInProject(project1, file2).Should().BeTrue();
            this.testSubject.IsFileInProject(project1, file3).Should().BeFalse();
            this.testSubject.IsFileInProject(project2, file1).Should().BeFalse();
            this.testSubject.IsFileInProject(project2, file2).Should().BeFalse();
            this.testSubject.IsFileInProject(project2, file3).Should().BeFalse();
        }

        [TestMethod]
        public void IsFileInProject_DifferentCase()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("project1");
            const string existingFile = "FILENAME";
            const string newFile = "filename";
            project.AddOrGetFile(existingFile);

            // Act + Assert
            this.testSubject.IsFileInProject(project, newFile).Should().BeTrue();
        }

        [TestMethod]
        public void FindFileInProject_Missing_ReturnsNull()
        {
            var project = this.solutionMock.AddOrGetProject("project");
            project.AddOrGetFile("fileThatExists.txt");

            this.testSubject.FindFileInProject(project, "fileNotInTheProject.txt").Should().BeNull();
        }

        [TestMethod]
        public void FindFileInProject_ProjectArgCheck()
        {
            Action act = () => testSubject.FindFileInProject(null, "file");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("project");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("\t\n")]
        public void FindFileInProject_FileArgChecks(string fileName)
        {
            Action act = () => this.testSubject.FindFileInProject(new ProjectMock("project"), fileName);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("file");
        }

        [TestMethod]
        public void FindFileInProject_Exists_ReturnsProjectItem()
        {
            var project = this.solutionMock.AddOrGetProject("project");
            var itemId = project.AddOrGetFile("fileInProject");

            var file1ExtObj = Mock.Of<ProjectItem>();
            project.SetExtObjProperty(itemId, file1ExtObj);

            var actual = this.testSubject.FindFileInProject(project, "fileInProject");
            actual.Should().BeSameAs(file1ExtObj);
        }

        [TestMethod]
        public void FindFileInProject_ExistsButWrongExtObjType_ReturnsNull()
        {
            var project = this.solutionMock.AddOrGetProject("project");
            var itemId = project.AddOrGetFile("fileInProject");

            var extObjectItem = new object();
            project.SetExtObjProperty(itemId, extObjectItem);

            var actual = this.testSubject.FindFileInProject(project, "fileInProject");
            actual.Should().BeNull();
        }

        [TestMethod]
        public void GetSolutionItemsProject()
        {
            // Arrange
            const string SolutionItemsName = "Hello world";
            this.SetSolutionFolderName(SolutionItemsName);
            DTEMock dte = new DTEMock();
            this.serviceProvider.RegisterService(typeof(DTE), dte);
            dte.Solution = this.solutionMock;

            // Act
            Project project1 = this.testSubject.GetSolutionItemsProject(true);

            // Assert
            project1.Should().NotBeNull("Could not find the solution items project");
            project1.Name.Should().Be(SolutionItemsName, "Unexpected project name");
            this.solutionMock.Projects.Count().Should().Be(1, "Unexpected project count");
            project1.Should().Be(this.solutionMock.Projects.Single(), "Unexpected project");

            // Act, ask again (exists already)
            Project project2 = this.testSubject.GetSolutionItemsProject(false);

            // Assert
            project2.Should().Be(project1, "Should be the same project as in the first time");
        }

        [TestMethod]
        public void GetSolutionFolderProject_WhenFolderDoesntExistButForceCreate_ExpectsANonNullProject()
        {
            /// Arrange
            const string solutionFolderName = "SomeFolderName";
            DTEMock dte = new DTEMock();
            this.serviceProvider.RegisterService(typeof(DTE), dte);
            dte.Solution = this.solutionMock;

            // Act
            Project project1 = this.testSubject.GetSolutionFolderProject(solutionFolderName, true);

            // Assert
            project1.Should().NotBeNull("Could not find the solution items project");
            project1.Name.Should().Be(solutionFolderName, "Unexpected project name");
            this.solutionMock.Projects.Count().Should().Be(1, "Unexpected project count");
            project1.Should().Be(this.solutionMock.Projects.Single(), "Unexpected project");
        }

        [TestMethod]
        public void GetSolutionFolderProject_WhenFolderDoesntExistButDontForceCreate_ExpectsANullProject()
        {
            /// Arrange
            const string solutionFolderName = "SomeFolderName";
            DTEMock dte = new DTEMock();
            this.serviceProvider.RegisterService(typeof(DTE), dte);
            dte.Solution = this.solutionMock;

            // Act
            Project project1 = this.testSubject.GetSolutionFolderProject(solutionFolderName, false);

            // Assert
            project1.Should().BeNull("Could not find the solution items project");
        }

        [TestMethod]
        public void GetSolutionFolderProject_WhenCalledMultipleTimes_ReturnsSameProject()
        {
            // Arrange
            const string solutionFolderName = "SomeFolderName";
            DTEMock dte = new DTEMock();
            this.serviceProvider.RegisterService(typeof(DTE), dte);
            dte.Solution = this.solutionMock;
            Project project1 = this.testSubject.GetSolutionFolderProject(solutionFolderName, true);

            // Act, ask again (exists already)
            Project project2 = this.testSubject.GetSolutionFolderProject(solutionFolderName, false);

            // Assert
            project2.Should().Be(project1, "Should be the same project as in the first time");
        }

        [TestMethod]
        public void GetFilteredSolutionProjects()
        {
            ProjectMock csProject = this.solutionMock.AddOrGetProject("c#");
            csProject.SetExtObjProperty(VSConstants.VSITEMID_ROOT, csProject);
            csProject.ProjectKind = ProjectSystemHelper.CSharpProjectKind;
            ProjectMock vbProject = this.solutionMock.AddOrGetProject("vb.net");
            vbProject.SetExtObjProperty(VSConstants.VSITEMID_ROOT, vbProject);
            vbProject.ProjectKind = ProjectSystemHelper.VbProjectKind;
            ProjectMock otherProject = this.solutionMock.AddOrGetProject("other");
            otherProject.SetExtObjProperty(VSConstants.VSITEMID_ROOT, otherProject);
            otherProject.ProjectKind = "other";
            ProjectMock erronousProject = this.solutionMock.AddOrGetProject("err");
            erronousProject.SetExtObjProperty(VSConstants.VSITEMID_ROOT, null);
            erronousProject.ProjectKind = ProjectSystemHelper.VbProjectKind;
            // Filter out C#, keep VB
            projectFilter.MatchingProjects.Add(vbProject);

            // Act
            var actual = this.testSubject.GetFilteredSolutionProjects().ToArray();

            // Assert
            CollectionAssert.AreEqual(new[] { vbProject }, actual,
                "Unexpected projects: {0}", string.Join(", ", actual.Select(p => p.Name)));
        }

        [TestMethod]
        public void AddFileToProject_ProjectArgCheck()
        {
            Action act = () => this.testSubject.AddFileToProject(null, "file");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("project");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("\t\n")]
        public void AddFileToProject_FileArgCheck(string file)
        {
            Action act = () => this.testSubject.AddFileToProject(new ProjectMock("project"), file);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("file");
        }

        [TestMethod]
        public void AddFileToProject()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("project1");
            const string fileToAdd = @"x:\myFile.txt";

            // Case 1: file not in project
            // Act
            this.testSubject.AddFileToProject(project, fileToAdd);

            // Assert
            project.Files.ContainsKey(fileToAdd).Should().BeTrue();

            // Case 2: file already in project
            // Act
            this.testSubject.AddFileToProject(project, fileToAdd);

            // Assert
            project.Files.ContainsKey(fileToAdd).Should().BeTrue();
        }

        [TestMethod]
        public void RemoveFileFromProject_WhenFileExists_ExpectsFileToBeRemoved()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("project1");
            const string file = @"x:\myFile.txt";
            this.testSubject.AddFileToProject(project, file);
            project.Files.ContainsKey(file).Should().BeTrue();

            // Act
            this.testSubject.RemoveFileFromProject(project, file);

            // Assert
            project.Files.ContainsKey(file).Should().BeFalse("file should no longer be in the project");
        }

        [TestMethod]
        public void RemoveFileFromProject_WhenFileDoesntExist_ExpectsNothingToChange()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("project1");
            const string file = @"x:\myFile.txt";
            this.testSubject.AddFileToProject(project, file);
            project.Files.ContainsKey(file).Should().BeTrue();
            var oldCount = project.Files.Count;

            // Act
            this.testSubject.RemoveFileFromProject(project, "foo");

            // Assert
            project.Files.ContainsKey(file).Should().BeTrue("file should still be in the project");
            project.Files.Should().HaveCount(oldCount, "file count should not have changed");
        }

        [TestMethod]
        public void RemoveFileFromProject_WhenFileExistsAndProjectIsNotSolutionFolder_ExpectsFileToBeRemovedAndProjectNotRemoved()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("project1");
            const string file = @"x:\myFile.txt";
            this.testSubject.AddFileToProject(project, file);
            project.Files.ContainsKey(file).Should().BeTrue();

            // Act
            this.testSubject.RemoveFileFromProject(project, file);

            // Assert
            project.Files.ContainsKey(file).Should().BeFalse("file should no longer be in project");
            project.Files.Should().BeEmpty("project should have no files");
            this.solutionMock.Projects.Contains(project).Should().BeTrue("project should still be in solution");
        }

        [TestMethod]
        public void RemoveFileFromProject_WhenFileExistsAndProjectIsSolutionFolder_ExpectsFileToBeRemovedAndProjectRemoved()
        {
            // Arrange
            DTEMock dte = new DTEMock();
            this.serviceProvider.RegisterService(typeof(DTE), dte);
            dte.Solution = this.solutionMock;
            var project = this.testSubject.GetSolutionFolderProject("foo", true);
            const string file = @"x:\myFile.txt";
            this.testSubject.AddFileToProject(project, file);
            this.testSubject.IsFileInProject(project, file).Should().BeTrue();

            // Act
            this.testSubject.RemoveFileFromProject(project, file);

            // Assert
            project.ProjectItems.Should().BeEmpty("project should have no files");
            this.solutionMock.Projects.Contains(project).Should().BeFalse("project should no longer be in solution");
        }

        [TestMethod]
        public void GetSelectedProjects_ReturnsActiveProjects()
        {
            // Arrange
            var dte = new DTEMock();
            this.serviceProvider.RegisterService(typeof(DTE), dte);

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
        public void GetProjectProperty_ProjectArgCheck()
        {
            Action act = () => testSubject.GetProjectProperty(null, "prop");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("dteProject");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("\t\n")]
        public void GetProjectProperty_PropertyNameArgCheck(string propertyName)
        {
            Action act = () => testSubject.GetProjectProperty(new ProjectMock("a.proj"), propertyName);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("propertyName");
        }

        [TestMethod]
        public void GetProjectProperty_PropertyDoesNotExist_ReturnsNull()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("my.proj");

            // Act
            var actualValue = testSubject.GetProjectProperty(project, "myprop");

            // Assert
            actualValue.Should().BeNull("Expected no property value to be returned");
        }

        [TestMethod]
        public void GetProjectProperty_PropertyExists_ReturnsValue()
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
        public void SetProjectProperty_PropertyArgCheck()
        {
            // 1. Null project
            Action act = () => testSubject.SetProjectProperty(null, "prop", "val");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("dteProject");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void SetProjectProperty_PropertyArgCheck(string propertyName)
        {
            Action act = () => testSubject.SetProjectProperty(new ProjectMock("a.proj"), propertyName, "val");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("propertyName");
        }

        [TestMethod]
        public void SetProjectProperty_PropertyDoesNotExist_AddsPropertyWithValue()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("my.proj");

            // Act
            testSubject.SetProjectProperty(project, "myprop", "myval");

            // Assert
            project.GetBuildProperty("myprop").Should().Be("myval", "Unexpected property value");
        }

        [TestMethod]
        public void SetProjectProperty_PropertyExists_OverwritesValue()
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
        public void ClearProjectProperty_ProjectArgCheck()
        {
            Action act = () => testSubject.ClearProjectProperty(null, "prop");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("dteProject");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("\t\n")]
        public void ClearProjectProperty_PropertyArgCheck(string propertyName)
        {
            Action act = () => testSubject.ClearProjectProperty(new ProjectMock("a.proj"), propertyName);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("propertyName");
        }

        [TestMethod]
        public void ClearProjectProperty_PropertyExists_ClearsProperty()
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
        public void GetProject_ArgChecks()
        {
            // Arrange
            Action act = () => testSubject.GetProject(null);

            // Act and assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("projectHierarchy");
        }

        [TestMethod]
        public void GetAggregateProjectKinds_ArgChecks()
        {
            // Arrange
            Action act = () => this.testSubject.GetAggregateProjectKinds(null).FirstOrDefault();

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("hierarchy");
        }

        [TestMethod]
        public void GetAggregateProjectKinds_NoGuids_ReturnsEmpty()
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
        public void GetAggregateProjectKinds_HasGoodAndBadGuids_ReturnsSuccessfullyParsedGuidsOnly()
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
        public void IsSolutionFullyLoaded_PropertyIsTrue_ReturnsTrue()
        {
            // Arrange
            this.solutionMock.IsFullyLoaded = true;

            // Act
            bool result = this.testSubject.IsSolutionFullyOpened();

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsSolutionFullyLoaded_PropertyIsFalse_ReturnsFalse()
        {
            // Arrange
            this.solutionMock.IsFullyLoaded = false;

            // Act
            bool result = this.testSubject.IsSolutionFullyOpened();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsSolutionFullyLoaded_PropertyIsNotBoolean_ReturnsFalse()
        {
            // Arrange
            this.solutionMock.IsFullyLoaded = "not a boolean";

            // Act
            bool result = this.testSubject.IsSolutionFullyOpened();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsLegacyProject_NotLegacy_ReturnsFalse()
        {
            // Arrange
            var mockProject = this.solutionMock.AddOrGetProject("dummy.proj", isLegacy: false);

            // Act
            var result = this.testSubject.IsLegacyProjectSystem(mockProject);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsLegacyProject_Legacy_ReturnsTrue()
        {
            // Arrange
            var mockProject  = this.solutionMock.AddOrGetProject("dummy.proj", isLegacy: true);

            // Act
            var result = this.testSubject.IsLegacyProjectSystem(mockProject);

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void EnumerateProjects_NoProjectsInSolution_EmptyList()
        {
            var result = testSubject.EnumerateProjects().ToArray();
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void EnumerateProjects_OneProjectInSolution_ListWithProject()
        {
            var project = solutionMock.AddOrGetProject("test.csproj", true, false);

            var result = testSubject.EnumerateProjects().ToArray();
            result.Should().BeEquivalentTo(project);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void EnumerateProjects_ProjectAndFolderInSolution_ListWithProjectAndFolder(bool isProjectUnderFolder)
        {
            var folder = solutionMock.AddOrGetProject("folder");
            folder.SetExtObjProperty(VSConstants.VSITEMID_ROOT, folder);

            var project = solutionMock.AddOrGetProject("test.csproj", true, false);
            project.SetExtObjProperty(VSConstants.VSITEMID_ROOT, isProjectUnderFolder ? folder : project);
            project.ProjectKind = ProjectSystemHelper.CSharpProjectKind;

            var result = testSubject.EnumerateProjects().ToArray();
            result.Should().BeEquivalentTo(folder, project);
        }

        [TestMethod]
        public void GetItemFilePath_ItemDoesNotExist_Null()
        {
            var project = new ProjectMock("test.csproj");

            using (new AssertIgnoreScope())
            {
                var result = testSubject.GetItemFilePath(project, (VSConstants.VSITEMID)123);
                result.Should().BeNull();
            }
        }

        [TestMethod]
        public void GetItemFilePath_ItemExists_ReturnsItemFilePath()
        {
            var project = new ProjectMock("test.csproj");
            var itemId = project.AddOrGetFile("test.cs");

            var result = testSubject.GetItemFilePath(project, (VSConstants.VSITEMID) itemId);
            result.Should().Be("test.cs");
        }

        [TestMethod]
        public void GetItemFilePath_ProjectPath_ReturnsProjectPath()
        {
            var project = new ProjectMock("test.csproj");

            var result = testSubject.GetItemFilePath(project, VSConstants.VSITEMID.Root);
            result.Should().Be("test.csproj");
        }

        [TestMethod]
        public void GetAllItems_ProjectIsEmpty_EmptyList()
        {
            var project = new ProjectMock("test.csproj");

            var result = testSubject.GetAllItems(project);
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetAllItems_MultipleFiles_FlatListOfAllItems()
        {
            var project = new ProjectMock("test.csproj");
            var file1 = project.AddOrGetFile("file1.cs");
            var file2 = project.AddOrGetFile("file2.cs");
            var file3 = project.AddOrGetFile("file3.cs");
            var file4 = project.AddOrGetFile("file4.cs");
            var folder1 = project.AddOrGetFile("folder1");
            var folder2 = project.AddOrGetFile("folder2");

            /*
             * file1
             * folder1
             *    file2
             *    folder2
             *       file3
             * file4
             */
            project.SetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_FirstChild, (int)file1);
            project.SetProperty(file1, (int)__VSHPROPID.VSHPROPID_NextSibling, (int)folder1);
            project.SetProperty(folder1, (int)__VSHPROPID.VSHPROPID_NextSibling, (int)file4);
            project.SetProperty(folder1, (int)__VSHPROPID.VSHPROPID_FirstChild, (int)file2);
            project.SetProperty(file2, (int)__VSHPROPID.VSHPROPID_NextSibling, (int)folder2);
            project.SetProperty(folder2, (int)__VSHPROPID.VSHPROPID_FirstChild, (int)file3);

            var result = testSubject.GetAllItems(project);
            var expected = new[] {file1, folder1, file2, folder2, file3, file4};
            result.Should().BeEquivalentTo(expected, c => c.WithStrictOrdering());
        }

        #endregion Tests

        #region Helpers

        private static ProjectSystemHelper CreateTestSubject(IVsSolution solution)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsSolution))).Returns(solution);

            var testSubject = new ProjectSystemHelper(serviceProvider.Object);
            return testSubject;
        }

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
                guidPackage.Should().Be(VSConstants.CLSID.VsEnvironmentPackage_guid, "Unexpected package");
                resid.Should().Be(ProjectSystemHelper.SolutionItemResourceId, "Unexpected resource id");
                pbstrOut = this.LoadPackageStringResult;
                return VSConstants.S_OK;
            }

            int IVsShell.LoadUILibrary(ref Guid guidPackage, uint dwExFlags, out uint phinstOut)
            {
                throw new NotImplementedException();
            }

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
