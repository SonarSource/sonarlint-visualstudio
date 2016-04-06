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
            Project project1 = this.testSubject.GetSolutionItemsProject();

            // Verify
            Assert.IsNotNull(project1, "Could not find the solution items project");
            Assert.AreEqual(SolutionItemsName, project1.Name, "Unexpected project name");
            Assert.AreEqual(1, this.solutionMock.Projects.Count(), "Unexpected project count");
            Assert.AreSame(this.solutionMock.Projects.Single(), project1, "Unexpected project");

            // Act, ask again (exists already)
            Project project2 = this.testSubject.GetSolutionItemsProject();

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
