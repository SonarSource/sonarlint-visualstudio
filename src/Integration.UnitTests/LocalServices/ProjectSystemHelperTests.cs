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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Linq;
using Xunit;

namespace SonarLint.VisualStudio.Integration.UnitTests
{

    public class ProjectSystemHelperTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private SolutionMock solutionMock;
        private ProjectSystemHelper testSubject;
        private ConfigurableProjectSystemFilter projectFilter;

        public ProjectSystemHelperTests()
        {
            this.solutionMock = new SolutionMock();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.serviceProvider.RegisterService(typeof(SVsSolution), this.solutionMock);
            this.testSubject = new ProjectSystemHelper(this.serviceProvider);

            this.projectFilter = new ConfigurableProjectSystemFilter();
            this.serviceProvider.RegisterService(typeof(IProjectSystemFilter), this.projectFilter);
        }

        #region Tests
        [Fact]
        public void Ctor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => new ProjectSystemHelper(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [Fact]
        public void GetIVsHierarchy_WithNullProject_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => this.testSubject.GetIVsHierarchy(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("project");
        }

        [Fact]
        public void ProjectSystemHelper_GetIVsHierarchy()
        {
            // Arrange
            string projectName = "project";

            // Sanity
            this.testSubject.GetIVsHierarchy(new ProjectMock(projectName))
                .Should().BeNull("Project not associated with the solution");

            ProjectMock project = this.solutionMock.AddOrGetProject(projectName);

            // Act
            IVsHierarchy h = this.testSubject.GetIVsHierarchy(project);

            // Assert
            h.Should().BeSameAs(project, "The test implementation of a ProjectMock is also the one for its IVsHierarcy");
        }

        [Fact]
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
            otherProject.ProjectKind = "other";
            ProjectMock erronousProject = this.solutionMock.AddOrGetProject("err");
            erronousProject.SetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, null);
            erronousProject.ProjectKind = ProjectSystemHelper.VbProjectKind;

            // Act
            var actual = this.testSubject.GetSolutionProjects().ToArray();

            // Assert
            actual.Should().Equal(new[] { csProject, vbProject },
                "Unexpected projects: {0}", string.Join(", ", actual.Select(p => p.Name)));
        }

        [Fact]
        public void IsFileInProject_WithNullProject_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => this.testSubject.IsFileInProject(null, "file");

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("project");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void IsFileInProject_WithNullOrEmptyOrWhiteSpaceFile_ThrowsArgumentNullException(string value)
        {
            // Arrange + Act
            Action act = () => this.testSubject.IsFileInProject(new ProjectMock("project"), value);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("file");
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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
            this.solutionMock.Projects.Should().HaveCount(1, "Unexpected project count");
            project1.Should().BeSameAs(this.solutionMock.Projects.Single(), "Unexpected project");

            // Act, ask again (exists already)
            Project project2 = this.testSubject.GetSolutionItemsProject(false);

            // Assert
            project2.Should().BeSameAs(project1, "Should be the same project as in the first time");
        }

        [Fact]
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
            this.solutionMock.Projects.Should().HaveCount(1, "Unexpected project count");
            project1.Should().BeSameAs(this.solutionMock.Projects.Single(), "Unexpected project");
        }

        [Fact]
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

        [Fact]
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
            project2.Should().BeSameAs(project1, "Should be the same project as in the first time");
        }

        [Fact]
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
            actual.Should().Equal(new[] { vbProject },
                "Unexpected projects: {0}", string.Join(", ", actual.Select(p => p.Name)));
        }

        [Fact]
        public void AddFileToProject_WithNullProject_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => this.testSubject.AddFileToProject(null, "file");

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("project");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void AddFileToProject_WithNullOrEmptyOrWhiteSpaceFullFilePath_ThrowsArgumentNullException(string value)
        {
            // Arrange + Act
            Action act = () => this.testSubject.AddFileToProject(new ProjectMock("project"), value);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("fullFilePath");
        }

        [Fact]
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

        [Fact]
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
            project.Files.ContainsKey(file)
                .Should().BeFalse("file should no longer be in the project");
        }

        [Fact]
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

        [Fact]
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
            project.Files.ContainsKey(file)
                .Should().BeFalse("file should no longer be in project");
            project.Files.Should().BeEmpty("project should have no files");
            this.solutionMock.Projects.Contains(project).Should().BeTrue("project should still be in solution");
        }

        [Fact]
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
            this.solutionMock.Projects.Contains(project)
                .Should().BeFalse("project should no longer be in solution");
        }

        [Fact]
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
            actualProjects.Should().Equal(expectedProjects, "Unexpected projects");
        }

        [Fact]
        public void GetProjectProperty_WithNullProject_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => testSubject.GetProjectProperty(null, "prop");

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GetProjectProperty_WithNullOrEmptyOrWhiteSpacePropertyName_ThrowsArgumentNullException(string value)
        {
            // Arrange + Act
            Action act = () => testSubject.GetProjectProperty(new ProjectMock("a.proj"), value);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ProjectSystemHelper_GetProjectProperty_PropertyDoesNotExist_ReturnsNull()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("my.proj");

            // Act
            var actualValue = testSubject.GetProjectProperty(project, "myprop");

            // Assert
            actualValue.Should().BeNull("Expected no property value to be returned");
        }

        [Fact]
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

        [Fact]
        public void SetProjectProperty_WithNullProject_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => testSubject.SetProjectProperty(null, "prop", "val");

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void SetProjectProperty_WithNullOrEmptyOrWhiteSpacePropertyName_ThrowsArgumentNullException(string value)
        {
            // Arrange + Act
            Action act = () => testSubject.SetProjectProperty(new ProjectMock("a.proj"), value, "val");

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ProjectSystemHelper_SetProjectProperty_PropertyDoesNotExist_AddsPropertyWithValue()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("my.proj");

            // Act
            testSubject.SetProjectProperty(project, "myprop", "myval");

            // Assert
            project.GetBuildProperty("myprop")
                .Should().Be("myval", "Unexpected property value");
        }

        [Fact]
        public void ProjectSystemHelper_SetProjectProperty_PropertyExists_OverwritesValue()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("my.proj");

            project.SetBuildProperty("myprop", "oldval");

            // Act
            testSubject.SetProjectProperty(project, "myprop", "newval");

            // Assert
            project.GetBuildProperty("myprop")
                .Should().Be("newval", "Unexpected property value");
        }

        [Fact]
        public void ProjectSystemHelper_ClearProjectProperty_ArgChecks()
        {
            // Arrange + Act
            Action act = () => testSubject.ClearProjectProperty(null, "prop");

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void ProjectSystemHelper_ClearProjectProperty_ArgChecks(string value)
        {
            // Arrange + Act
            Action act = () => testSubject.ClearProjectProperty(new ProjectMock("a.proj"), value);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ProjectSystemHelper_ClearProjectProperty_PropertyExists_ClearsProperty()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("my.proj");

            project.SetBuildProperty("myprop", "val");

            // Act
            testSubject.ClearProjectProperty(project, "myprop");

            // Assert
            project.GetBuildProperty("myprop")
                .Should().BeNull("Expected property value to be cleared");
        }


        [Fact]
        public void GetAggregateProjectKinds_WithNullVsHierarchy_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => this.testSubject.GetAggregateProjectKinds(null).FirstOrDefault();

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ProjectSystemHelper_GetAggregateProjectKinds_NoGuids_ReturnsEmpty()
        {
            // Arrange
            var project = new ProjectMock("my.project");
            project.SetAggregateProjectTypeString(string.Empty);

            // Act
            Guid[] actualGuids = this.testSubject.GetAggregateProjectKinds(project).ToArray();

            // Assert
            actualGuids.Should().BeEmpty("Expected no GUIDs returned");
        }

        [Fact]
        public void ProjectSystemHelper_GetAggregateProjectKinds_HasGoodAndBadGuids_ReturnsSuccessfullyParsedGuidsOnly()
        {
            // Arrange
            string guidString = ";;;F602148F607646F88F7772CC9C49BC3F;;__BAD__;;__BADGUID__;0BA323B301614B1C80D74607B7EB7F5A;;;__FOO__;;;";
            Guid[] expectedGuids = new[]
            {
                new Guid("F602148F607646F88F7772CC9C49BC3F"),
                new Guid("0BA323B301614B1C80D74607B7EB7F5A"),
            };

            var project = new ProjectMock("my.project");
            project.SetAggregateProjectTypeString(guidString);

            // Act
            Guid[] actualGuids = this.testSubject.GetAggregateProjectKinds(project).ToArray();

            // Assert
            actualGuids.Should().Equal(expectedGuids, "Unexpected project kind GUIDs returned");
        }

        #endregion

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
            #endregion
        }
        #endregion

    }
}
