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
using System.IO.Abstractions;
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
        private Mock<IFileSystem> fileSystem;

        [TestInitialize]
        public void TestInitialize()
        {
            fileSystem = new Mock<IFileSystem>();
            solutionMock = new SolutionMock();

            serviceProvider = new ConfigurableServiceProvider();
            serviceProvider.RegisterService(typeof(SVsSolution), solutionMock);

            projectFilter = new ConfigurableProjectSystemFilter();
            serviceProvider.RegisterService(typeof(IProjectSystemFilter), projectFilter);

            testSubject = new ProjectSystemHelper(serviceProvider, fileSystem.Object);
        }

        #region Tests

        [TestMethod]
        public void ProjectSystemHelper_ArgCheck()
        {
            Action act = () => new ProjectSystemHelper(null, fileSystem.Object);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");

            act = () => new ProjectSystemHelper(serviceProvider, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

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
            string projectName = "project";

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
            csProject.SetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, csProject);
            csProject.ProjectKind = ProjectSystemHelper.CSharpProjectKind;
            ProjectMock vbProject = this.solutionMock.AddOrGetProject("vb.net");
            vbProject.SetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, vbProject);
            vbProject.ProjectKind = ProjectSystemHelper.VbProjectKind;
            ProjectMock otherProject = this.solutionMock.AddOrGetProject("other");
            otherProject.SetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, otherProject);
            otherProject.ProjectKind ="other";
            ProjectMock erronousProject = this.solutionMock.AddOrGetProject("err");
            erronousProject.SetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, null);
            erronousProject.ProjectKind = ProjectSystemHelper.VbProjectKind;

            // Act
            var actual = this.testSubject.GetSolutionProjects().ToArray();

            // Assert
            CollectionAssert.AreEqual(new[] { csProject, vbProject }, actual,
                "Unexpected projects: {0}", string.Join(", ", actual.Select(p => p.Name)));
        }

        [TestMethod]
        public void ProjectSystemHelper_IsFileInProject_ArgChecks()
        {
            // 1. Invalid project
            Action act = () => this.testSubject.IsFileInProject(null, "file");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("project");

            // 2. Null file name
            act = () => this.testSubject.IsFileInProject(new ProjectMock("project"), null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("file");

            // 3. Empty file name
            act = () => this.testSubject.IsFileInProject(new ProjectMock("project"), "");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("file");

            // 4. Null file name
            act = () => this.testSubject.IsFileInProject(new ProjectMock("project"), "\t\n");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("file");
        }

        [TestMethod]
        public void ProjectSystemHelper_IsFileInProject()
        {
            // Arrange
            ProjectMock project1 = this.solutionMock.AddOrGetProject("project1");
            ProjectMock project2 = this.solutionMock.AddOrGetProject("project2");
            string file1 = "file1";
            string file2 = "file2";
            string file3 = "file3";
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
        public void ProjectSystemHelper_IsFileInProject_DifferentCase()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("project1");
            string existingFile = "FILENAME";
            string newFile = "filename";
            project.AddOrGetFile(existingFile);

            // Act + Assert
            this.testSubject.IsFileInProject(project, newFile).Should().BeTrue();
        }

        [TestMethod]
        public void ProjectSystemHelper_GetSolutionItemsProject()
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
            var solutionFolderName = "SomeFolderName";
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
            var solutionFolderName = "SomeFolderName";
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
            var solutionFolderName = "SomeFolderName";
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
        public void ProjectSystemHelper_GetFilteredSolutionProjects()
        {
            ProjectMock csProject = this.solutionMock.AddOrGetProject("c#");
            csProject.SetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, csProject);
            csProject.ProjectKind = ProjectSystemHelper.CSharpProjectKind;
            ProjectMock vbProject = this.solutionMock.AddOrGetProject("vb.net");
            vbProject.SetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, vbProject);
            vbProject.ProjectKind = ProjectSystemHelper.VbProjectKind;
            ProjectMock otherProject = this.solutionMock.AddOrGetProject("other");
            otherProject.SetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, otherProject);
            otherProject.ProjectKind = "other";
            ProjectMock erronousProject = this.solutionMock.AddOrGetProject("err");
            erronousProject.SetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, null);
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
        public void ProjectSystemHelper_AddFileToProject_ArgChecks()
        {
            // 1. Invalid project
            Action act = () => this.testSubject.AddFileToProject(null, "file");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("project");

            // 2. Null file name
            act = () => this.testSubject.AddFileToProject(new ProjectMock("project"), null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("file");

            // 3. Empty file name
            act = () => this.testSubject.AddFileToProject(new ProjectMock("project"), "");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("file");

            // 4. Whitespace file name
            act = () => this.testSubject.AddFileToProject(new ProjectMock("project"), "\t\n");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("file");
        }

        [TestMethod]
        public void ProjectSystemHelper_AddFileToProject()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("project1");
            string fileToAdd = @"x:\myFile.txt";

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
            string file = @"x:\myFile.txt";
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
            string file = @"x:\myFile.txt";
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
            string file = @"x:\myFile.txt";
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
            string file = @"x:\myFile.txt";
            this.testSubject.AddFileToProject(project, file);
            this.testSubject.IsFileInProject(project, file).Should().BeTrue();

            // Act
            this.testSubject.RemoveFileFromProject(project, file);

            // Assert
            project.ProjectItems.Should().BeEmpty("project should have no files");
            this.solutionMock.Projects.Contains(project).Should().BeFalse("project should no longer be in solution");
        }

        [TestMethod]
        public void ProjectSystemHelper_GetSelectedProjects_ReturnsActiveProjects()
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
        public void ProjectSystemHelper_GetProjectProperty_ArgChecks()
        {
            // 1. Null project
            Action act = () => testSubject.GetProjectProperty(null, "prop");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("dteProject");

            // 2. Null property name
            act = () => testSubject.GetProjectProperty(new ProjectMock("a.proj"), null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("propertyName");

            // 3. Empty property name
            act = () => testSubject.GetProjectProperty(new ProjectMock("a.proj"), string.Empty);
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
        public void ProjectSystemHelper_SetProjectProperty_ArgChecks()
        {
            // 1. Null project
            Action act = () => testSubject.SetProjectProperty(null, "prop", "val");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("dteProject");

            // 2. Null property name
            act = () => testSubject.SetProjectProperty(new ProjectMock("a.proj"), null, "val");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("propertyName");

            // 3. Empty property name
            act = () => testSubject.SetProjectProperty(new ProjectMock("a.proj"), string.Empty, "val");
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
        public void ProjectSystemHelper_ClearProjectProperty_ArgChecks()
        {
            // 1. Null project
            Action act = () => testSubject.ClearProjectProperty(null, "prop");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("dteProject");

            // 2. Null property name
            act = () => testSubject.ClearProjectProperty(new ProjectMock("a.proj"), null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("propertyName");

            // 3. Empty property name
            act = () => testSubject.ClearProjectProperty(new ProjectMock("a.proj"), string.Empty);
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
            string guidString = ";;;F602148F607646F88F7772CC9C49BC3F;;__BAD__;;__BADGUID__;0BA323B301614B1C80D74607B7EB7F5A;;;__FOO__;;;";
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
        public void ProjectSystemHelper_IsSolutionFullyLoaded_PropertyIsTrue_ReturnsTrue()
        {
            // Arrange
            this.solutionMock.IsFullyLoaded = true;

            // Act
            bool result = this.testSubject.IsSolutionFullyOpened();

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void ProjectSystemHelper_IsSolutionFullyLoaded_PropertyIsFalse_ReturnsFalse()
        {
            // Arrange
            this.solutionMock.IsFullyLoaded = false;

            // Act
            bool result = this.testSubject.IsSolutionFullyOpened();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void ProjectSystemHelper_IsSolutionFullyLoaded_PropertyIsNotBoolean_ReturnsFalse()
        {
            // Arrange
            this.solutionMock.IsFullyLoaded = "not a boolean";

            // Act
            bool result = this.testSubject.IsSolutionFullyOpened();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void ProjectSystemHelper_IsLegacyProject_NotLegacy_ReturnsFalse()
        {
            // Arrange
            var mockProject = this.solutionMock.AddOrGetProject("dummy.proj", isLegacy: false);

            // Act
            var result = this.testSubject.IsLegacyProjectSystem(mockProject);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void ProjectSystemHelper_IsLegacyProject_Legacy_ReturnsTrue()
        {
            // Arrange
            var mockProject  = this.solutionMock.AddOrGetProject("dummy.proj", isLegacy: true);

            // Act
            var result = this.testSubject.IsLegacyProjectSystem(mockProject);

            // Assert
            result.Should().BeTrue();
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

        [TestMethod]
        public void DoesExistInItemGroup_ProjectHasNoItemGroups_False()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
</Project>";

            var project = new ProjectMock("test.csproj");
            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.DoesExistInItemGroup(project, "MyGroup", "MyValue");
            actual.Should().BeFalse();
        }

        [TestMethod]
        public void DoesExistInItemGroup_ProjectItemGroupHasNoIncludes_False()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
  </ItemGroup>
</Project>";

            var project = new ProjectMock("test.csproj");
            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.DoesExistInItemGroup(project, "MyGroup", "MyValue");
            actual.Should().BeFalse();
        }

        [TestMethod]
        public void DoesExistInItemGroup_ProjectItemGroupHasIncorrectInclude_False()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
   <SomeOtherGroup Include=""MyValue"" />
  </ItemGroup>
</Project>";

            var project = new ProjectMock("test.csproj");
            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.DoesExistInItemGroup(project, "MyGroup", "MyValue");
            actual.Should().BeFalse();
        }

        [TestMethod]
        public void DoesExistInItemGroup_ProjectHasOneCorrectInclude_IncorrectValue_False()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <MyGroup Include=""some other value"" />
  </ItemGroup>
</Project>";

            var project = new ProjectMock("test.csproj");
            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.DoesExistInItemGroup(project, "MyGroup", "MyValue");
            actual.Should().BeFalse();
        }

        [TestMethod]
        public void DoesExistInItemGroup_ProjectHasMultipleCorrectIncludes_IncorrectValues_False()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <MyGroup Include=""MyValue1"" />
    <MyGroup Include=""MyValue2"" />
  </ItemGroup>
</Project>";

            var project = new ProjectMock("test.csproj");
            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.DoesExistInItemGroup(project, "MyGroup", "MyValue");
            actual.Should().BeFalse();
        }

        [TestMethod]
        public void DoesExistInItemGroup_ProjectHasOneCorrectIncludeThatIsCommentedOut_False()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <!--<MyGroup Include=""MyValue"" />-->
</ItemGroup>
</Project>";

            var project = new ProjectMock("test.csproj");
            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.DoesExistInItemGroup(project, "MyGroup", "MyValue");
            actual.Should().BeFalse();
        }

        [TestMethod]
        public void DoesExistInItemGroup_ProjectHasOneCorrectInclude_CorrectValue_True()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <MyGroup Include=""MyValue"" />
  </ItemGroup>
</Project>";

            var project = new ProjectMock("test.csproj");
            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.DoesExistInItemGroup(project, "MyGroup", "MyValue");
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void DoesExistInItemGroup_ProjectHasMultipleCorrectIncludes_HasCorrectValue_True()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <MyGroup Include=""MyValue"" />
    <MyGroup Include=""MyValue2"" />
  </ItemGroup>
</Project>";

            var project = new ProjectMock("test.csproj");
            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.DoesExistInItemGroup(project, "MyGroup", "MyValue");
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void DoesExistInItemGroup_ProjectHasMultipleCorrectIncludesInDifferentItemGroups_HasCorrectValue_True()
        {
            var projectXml = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <MyGroup Include=""MyValue1"" />
  </ItemGroup>
  <ItemGroup>
    <MyGroup Include=""MyValue"" />
  </ItemGroup>
</Project>";

            var project = new ProjectMock("test.csproj");
            fileSystem.Setup(x => x.File.ReadAllText(project.FilePath)).Returns(projectXml);

            var actual = testSubject.DoesExistInItemGroup(project, "MyGroup", "MyValue");
            actual.Should().BeTrue();
        }
    }
}
