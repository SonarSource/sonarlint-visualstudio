//-----------------------------------------------------------------------
// <copyright file="ProjectSystemHelperTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

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
        public void ProjectSystemHelper_ArgCheck()
        {
            Exceptions.Expect<ArgumentNullException>(() => new ProjectSystemHelper(null));
        }

        [TestMethod]
        public void ProjectSystemHelper_GetIVsHierarchy_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.GetIVsHierarchy(null));
        }

        [TestMethod]
        public void ProjectSystemHelper_GetIVsHierarchy()
        {
            // Setup
            string projectName = "project";

            // Sanity
            Assert.IsNull(this.testSubject.GetIVsHierarchy(new ProjectMock(projectName)), "Project not associated with the solution");

            ProjectMock project = this.solutionMock.AddOrGetProject(projectName);

            // Act
            IVsHierarchy h = this.testSubject.GetIVsHierarchy(project);

            // Verify
            Assert.AreSame(project, h, "The test implementation of a ProjectMock is also the one for its IVsHierarcy");
        }

        [TestMethod]
        public void ProjectSystemHelper_GetSolutionProjects_ReturnsOnlyKnownLanguages()
        {
            // Setup
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

            // Verify
            CollectionAssert.AreEqual(new[] { csProject, vbProject }, actual,
                "Unexpected projects: {0}", string.Join(", ", actual.Select(p => p.Name)));
        }

        [TestMethod]
        public void ProjectSystemHelper_IsFileInProject_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.IsFileInProject(null, "file"));
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.IsFileInProject(new ProjectMock("project"), null));
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.IsFileInProject(new ProjectMock("project"), ""));
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.IsFileInProject(new ProjectMock("project"), "\t\n"));
        }

        [TestMethod]
        public void ProjectSystemHelper_IsFileInProject()
        {
            // Setup
            ProjectMock project1 = this.solutionMock.AddOrGetProject("project1");
            ProjectMock project2 = this.solutionMock.AddOrGetProject("project2");
            string file1 = "file1";
            string file2 = "file2";
            string file3 = "file3";
            project1.AddOrGetFile(file1);
            project1.AddOrGetFile(file2);

            // Act + Verify
            Assert.IsTrue(this.testSubject.IsFileInProject(project1, file1));
            Assert.IsTrue(this.testSubject.IsFileInProject(project1, file2));
            Assert.IsFalse(this.testSubject.IsFileInProject(project1, file3));
            Assert.IsFalse(this.testSubject.IsFileInProject(project2, file1));
            Assert.IsFalse(this.testSubject.IsFileInProject(project2, file2));
            Assert.IsFalse(this.testSubject.IsFileInProject(project2, file3));
        }

        [TestMethod]
        public void ProjectSystemHelper_IsFileInProject_DifferentCase()
        {
            // Setup
            ProjectMock project = this.solutionMock.AddOrGetProject("project1");
            string existingFile = "FILENAME";
            string newFile = "filename";
            project.AddOrGetFile(existingFile);

            // Act + Verify
            Assert.IsTrue(this.testSubject.IsFileInProject(project, newFile));
        }

        [TestMethod]
        public void ProjectSystemHelper_GetSolutionItemsProject()
        {
            // Setup
            const string SolutionItemsName = "Hello world";
            this.SetSolutionFolderName(SolutionItemsName);
            DTEMock dte = new DTEMock();
            this.serviceProvider.RegisterService(typeof(DTE), dte);
            dte.Solution = this.solutionMock;

            // Act
            Project project1 = this.testSubject.GetSolutionItemsProject(true);

            // Verify
            Assert.IsNotNull(project1, "Could not find the solution items project");
            Assert.AreEqual(SolutionItemsName, project1.Name, "Unexpected project name");
            Assert.AreEqual(1, this.solutionMock.Projects.Count(), "Unexpected project count");
            Assert.AreSame(this.solutionMock.Projects.Single(), project1, "Unexpected project");

            // Act, ask again (exists already)
            Project project2 = this.testSubject.GetSolutionItemsProject(false);

            // Verify
            Assert.AreSame(project1, project2, "Should be the same project as in the first time");
        }

        [TestMethod]
        public void GetSolutionFolderProject_WhenFolderDoesntExistButForceCreate_ExpectsANonNullProject()
        {
            /// Setup
            var solutionFolderName = "SomeFolderName";
            DTEMock dte = new DTEMock();
            this.serviceProvider.RegisterService(typeof(DTE), dte);
            dte.Solution = this.solutionMock;

            // Act
            Project project1 = this.testSubject.GetSolutionFolderProject(solutionFolderName, true);

            // Verify
            Assert.IsNotNull(project1, "Could not find the solution items project");
            Assert.AreEqual(solutionFolderName, project1.Name, "Unexpected project name");
            Assert.AreEqual(1, this.solutionMock.Projects.Count(), "Unexpected project count");
            Assert.AreSame(this.solutionMock.Projects.Single(), project1, "Unexpected project");
        }

        [TestMethod]
        public void GetSolutionFolderProject_WhenFolderDoesntExistButDontForceCreate_ExpectsANullProject()
        {
            /// Setup
            var solutionFolderName = "SomeFolderName";
            DTEMock dte = new DTEMock();
            this.serviceProvider.RegisterService(typeof(DTE), dte);
            dte.Solution = this.solutionMock;

            // Act
            Project project1 = this.testSubject.GetSolutionFolderProject(solutionFolderName, false);

            // Verify
            Assert.IsNull(project1, "Could not find the solution items project");
        }

        [TestMethod]
        public void GetSolutionFolderProject_WhenCalledMultipleTimes_ReturnsSameProject()
        {
            // Setup
            var solutionFolderName = "SomeFolderName";
            DTEMock dte = new DTEMock();
            this.serviceProvider.RegisterService(typeof(DTE), dte);
            dte.Solution = this.solutionMock;
            Project project1 = this.testSubject.GetSolutionFolderProject(solutionFolderName, true);


            // Act, ask again (exists already)
            Project project2 = this.testSubject.GetSolutionFolderProject(solutionFolderName, false);

            // Verify
            Assert.AreSame(project1, project2, "Should be the same project as in the first time");
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

            // Verify
            CollectionAssert.AreEqual(new[] { vbProject }, actual,
                "Unexpected projects: {0}", string.Join(", ", actual.Select(p => p.Name)));
        }

        [TestMethod]
        public void ProjectSystemHelper_AddFileToProject_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.AddFileToProject(null, "file"));
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.AddFileToProject(new ProjectMock("project"), null));
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.AddFileToProject(new ProjectMock("project"), ""));
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.AddFileToProject(new ProjectMock("project"), "\t\n"));
        }

        [TestMethod]
        public void ProjectSystemHelper_AddFileToProject()
        {
            // Setup
            ProjectMock project = this.solutionMock.AddOrGetProject("project1");
            string fileToAdd = @"x:\myFile.txt";

            // Case 1: file not in project
            // Act
            this.testSubject.AddFileToProject(project, fileToAdd);

            // Verify
            Assert.IsTrue(project.Files.ContainsKey(fileToAdd));

            // Case 2: file already in project
            // Act
            this.testSubject.AddFileToProject(project, fileToAdd);

            // Verify
            Assert.IsTrue(project.Files.ContainsKey(fileToAdd));
        }

        [TestMethod]
        public void RemoveFileFromProject_WhenFileExists_ExpectsFileToBeRemoved()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("project1");
            string file = @"x:\myFile.txt";
            this.testSubject.AddFileToProject(project, file);
            Assert.IsTrue(project.Files.ContainsKey(file));

            // Act
            this.testSubject.RemoveFileFromProject(project, file);

            // Assert
            Assert.IsFalse(project.Files.ContainsKey(file), "file should no longer be in the project");
        }

        [TestMethod]
        public void RemoveFileFromProject_WhenFileDoesntExist_ExpectsNothingToChange()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("project1");
            string file = @"x:\myFile.txt";
            this.testSubject.AddFileToProject(project, file);
            Assert.IsTrue(project.Files.ContainsKey(file));
            var oldCount = project.Files.Count;

            // Act
            this.testSubject.RemoveFileFromProject(project, "foo");

            // Assert
            Assert.IsTrue(project.Files.ContainsKey(file), "file should still be in the project");
            Assert.AreEqual(oldCount, project.Files.Count, "file count should not have changed");
        }

        [TestMethod]
        public void RemoveFileFromProject_WhenFileExistsAndProjectIsNotSolutionFolder_ExpectsFileToBeRemovedAndProjectNotRemoved()
        {
            // Arrange
            ProjectMock project = this.solutionMock.AddOrGetProject("project1");
            string file = @"x:\myFile.txt";
            this.testSubject.AddFileToProject(project, file);
            Assert.IsTrue(project.Files.ContainsKey(file));

            // Act
            this.testSubject.RemoveFileFromProject(project, file);

            // Assert
            Assert.IsFalse(project.Files.ContainsKey(file), "file should no longer be in project");
            Assert.AreEqual(0, project.Files.Count, "project should have no files");
            Assert.IsTrue(this.solutionMock.Projects.Contains(project), "project should still be in solution");
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
            Assert.IsTrue(this.testSubject.IsFileInProject(project, file));

            // Act
            this.testSubject.RemoveFileFromProject(project, file);

            // Assert
            Assert.AreEqual(0, project.ProjectItems.Count, "project should have no files");
            Assert.IsFalse(this.solutionMock.Projects.Contains(project), "project should no longer be in solution");
        }

        [TestMethod]
        public void ProjectSystemHelper_GetSelectedProjects_ReturnsActiveProjects()
        {
            // Setup
            var dte = new DTEMock();
            this.serviceProvider.RegisterService(typeof(DTE), dte);

            var p1 = new ProjectMock("p1.proj");
            var p2 = new ProjectMock("p1.proj");

            var expectedProjects = new Project[] { p1, p2 };
            dte.ActiveSolutionProjects = expectedProjects;

            // Act
            Project[] actualProjects = testSubject.GetSelectedProjects().ToArray();

            // Verify
            CollectionAssert.AreEquivalent(expectedProjects, actualProjects, "Unexpected projects");
        }

        [TestMethod]
        public void ProjectSystemHelper_GetProjectProperty_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => testSubject.GetProjectProperty(null, "prop"));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.GetProjectProperty(new ProjectMock("a.proj"), null));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.GetProjectProperty(new ProjectMock("a.proj"), string.Empty));
        }

        [TestMethod]
        public void ProjectSystemHelper_GetProjectProperty_PropertyDoesNotExist_ReturnsNull()
        {
            // Setup
            ProjectMock project = this.solutionMock.AddOrGetProject("my.proj");

            // Act
            var actualValue = testSubject.GetProjectProperty(project, "myprop");

            // Verify
            Assert.IsNull(actualValue, "Expected no property value to be returned");
        }

        [TestMethod]
        public void ProjectSystemHelper_GetProjectProperty_PropertyExists_ReturnsValue()
        {
            // Setup
            ProjectMock project = this.solutionMock.AddOrGetProject("my.proj");

            project.SetBuildProperty("myprop", "myval");

            // Act
            var actualValue = testSubject.GetProjectProperty(project, "myprop");

            // Verify
            Assert.AreEqual("myval", actualValue, "Unexpected property value");
        }
        [TestMethod]
        public void ProjectSystemHelper_SetProjectProperty_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => testSubject.SetProjectProperty(null, "prop", "val"));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.SetProjectProperty(new ProjectMock("a.proj"), null, "val"));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.SetProjectProperty(new ProjectMock("a.proj"), string.Empty, "val"));
        }

        [TestMethod]
        public void ProjectSystemHelper_SetProjectProperty_PropertyDoesNotExist_AddsPropertyWithValue()
        {
            // Setup
            ProjectMock project = this.solutionMock.AddOrGetProject("my.proj");

            // Act
            testSubject.SetProjectProperty(project, "myprop", "myval");

            // Verify
            Assert.AreEqual("myval", project.GetBuildProperty("myprop"), "Unexpected property value");
        }

        [TestMethod]
        public void ProjectSystemHelper_SetProjectProperty_PropertyExists_OverwritesValue()
        {
            // Setup
            ProjectMock project = this.solutionMock.AddOrGetProject("my.proj");

            project.SetBuildProperty("myprop", "oldval");

            // Act
            testSubject.SetProjectProperty(project, "myprop", "newval");

            // Verify
            Assert.AreEqual("newval", project.GetBuildProperty("myprop"), "Unexpected property value");
        }
        [TestMethod]
        public void ProjectSystemHelper_ClearProjectProperty_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => testSubject.ClearProjectProperty(null, "prop"));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.ClearProjectProperty(new ProjectMock("a.proj"), null));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.ClearProjectProperty(new ProjectMock("a.proj"), string.Empty));
        }

        [TestMethod]
        public void ProjectSystemHelper_ClearProjectProperty_PropertyExists_ClearsProperty()
        {
            // Setup
            ProjectMock project = this.solutionMock.AddOrGetProject("my.proj");

            project.SetBuildProperty("myprop", "val");

            // Act
            testSubject.ClearProjectProperty(project, "myprop");

            // Verify
            Assert.IsNull(project.GetBuildProperty("myprop"), "Expected property value to be cleared");
        }


        [TestMethod]
        public void ProjectSystemHelper_GetAggregateProjectKinds_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.GetAggregateProjectKinds(null).FirstOrDefault());
        }

        [TestMethod]
        public void ProjectSystemHelper_GetAggregateProjectKinds_NoGuids_ReturnsEmpty()
        {
            // Setup
            var project = new ProjectMock("my.project");
            project.SetAggregateProjectTypeString(string.Empty);

            // Act
            Guid[] actualGuids = this.testSubject.GetAggregateProjectKinds(project).ToArray();

            // Verify
            Assert.IsFalse(actualGuids.Any(), "Expected no GUIDs returned");
        }

        [TestMethod]
        public void ProjectSystemHelper_GetAggregateProjectKinds_HasGoodAndBadGuids_ReturnsSuccessfullyParsedGuidsOnly()
        {
            // Setup
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

            // Verify
            CollectionAssert.AreEquivalent(expectedGuids, actualGuids, "Unexpected project kind GUIDs returned");
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
                Assert.AreEqual(VSConstants.CLSID.VsEnvironmentPackage_guid, guidPackage, "Unexpected package");
                Assert.AreEqual(ProjectSystemHelper.SolutionItemResourceId, resid, "Unexpected resource id");
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
