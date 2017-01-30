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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public partial class SolutionMock : VsUIHierarchyMock, IVsSolution2
    {
        private readonly List<IVsSolutionEvents> sinks = new List<IVsSolutionEvents>();
        private readonly Dictionary<string, ProjectMock> projects = new Dictionary<string, ProjectMock>(StringComparer.OrdinalIgnoreCase);
        private readonly DTEMock dte;

        public SolutionMock(DTEMock dte = null)
            : base("Solution.sln")
        {
            this.dte = dte;
            if (dte != null)
            {
                dte.Solution = this;
            }
        }

        #region IVsSolution
        int IVsSolution.AddVirtualProject(IVsHierarchy pHierarchy, uint grfAddVPFlags)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.AddVirtualProject(IVsHierarchy pHierarchy, uint grfAddVPFlags)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.AddVirtualProjectEx(IVsHierarchy pHierarchy, uint grfAddVPFlags, ref Guid rguidProjectID)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.AddVirtualProjectEx(IVsHierarchy pHierarchy, uint grfAddVPFlags, ref Guid rguidProjectID)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.AdviseSolutionEvents(IVsSolutionEvents pSink, out uint pdwCookie)
        {
            pdwCookie = (uint)this.sinks.Count + 1;
            this.sinks.Add(pSink);
            return VSConstants.S_OK;
        }

        int IVsSolution2.AdviseSolutionEvents(IVsSolutionEvents pSink, out uint pdwCookie)
        {
            return ((IVsSolution)this).AdviseSolutionEvents(pSink, out pdwCookie);
        }

        int IVsSolution.CanCreateNewProjectAtLocation(int fCreateNewSolution, string pszFullProjectFilePath, out int pfCanCreate)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.CanCreateNewProjectAtLocation(int fCreateNewSolution, string pszFullProjectFilePath, out int pfCanCreate)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.CloseSolutionElement(uint grfCloseOpts, IVsHierarchy pHier, uint docCookie)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.CloseSolutionElement(uint grfCloseOpts, IVsHierarchy pHier, uint docCookie)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.CreateNewProjectViaDlg(string pszExpand, string pszSelect, uint dwReserved)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.CreateNewProjectViaDlg(string pszExpand, string pszSelect, uint dwReserved)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.CreateProject(ref Guid rguidProjectType, string lpszMoniker, string lpszLocation, string lpszName, uint grfCreateFlags, ref Guid iidProject, out IntPtr ppProject)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.CreateProject(ref Guid rguidProjectType, string lpszMoniker, string lpszLocation, string lpszName, uint grfCreateFlags, ref Guid iidProject, out IntPtr ppProject)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.CreateSolution(string lpszLocation, string lpszName, uint grfCreateFlags)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.CreateSolution(string lpszLocation, string lpszName, uint grfCreateFlags)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.GenerateNextDefaultProjectName(string pszBaseName, string pszLocation, out string pbstrProjectName)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.GenerateNextDefaultProjectName(string pszBaseName, string pszLocation, out string pbstrProjectName)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.GenerateUniqueProjectName(string lpszRoot, out string pbstrProjectName)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.GenerateUniqueProjectName(string lpszRoot, out string pbstrProjectName)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.GetGuidOfProject(IVsHierarchy pHierarchy, out Guid pguidProjectID)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.GetGuidOfProject(IVsHierarchy pHierarchy, out Guid pguidProjectID)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.GetItemInfoOfProjref(string pszProjref, int propid, out object pvar)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.GetItemInfoOfProjref(string pszProjref, int propid, out object pvar)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.GetItemOfProjref(string pszProjref, out IVsHierarchy ppHierarchy, out uint pitemid, out string pbstrUpdatedProjref, VSUPDATEPROJREFREASON[] puprUpdateReason)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.GetItemOfProjref(string pszProjref, out IVsHierarchy ppHierarchy, out uint pitemid, out string pbstrUpdatedProjref, VSUPDATEPROJREFREASON[] puprUpdateReason)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.GetProjectEnum(uint grfEnumFlags, ref Guid rguidEnumOnlyThisType, out IEnumHierarchies ppenum)
        {
            grfEnumFlags.Should().Be((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, "Unexpected argument value grfEnumFlags");
            ppenum = new EnumHierarchies(this);
            return VSConstants.S_OK;
        }

        int IVsSolution2.GetProjectEnum(uint grfEnumFlags, ref Guid rguidEnumOnlyThisType, out IEnumHierarchies ppenum)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.GetProjectFactory(uint dwReserved, Guid[] pguidProjectType, string pszMkProject, out IVsProjectFactory ppProjectFactory)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.GetProjectFactory(uint dwReserved, ref Guid pguidProjectType, string pszMkProject, out IVsProjectFactory ppProjectFactory)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.GetProjectFilesInSolution(uint grfGetOpts, uint cProjects, string[] rgbstrProjectNames, out uint pcProjectsFetched)
        {
            grfGetOpts.Should().Be((uint)__VSGETPROJFILESFLAGS.GPFF_SKIPUNLOADEDPROJECTS);

            string[] loadedProjects = this.projects.Values.Where(p => p.IsLoaded).Select(p => p.FilePath).ToArray();
            pcProjectsFetched = (uint)loadedProjects.Length;

            if (rgbstrProjectNames != null)
            {
                for (int i = 0; i < cProjects; i++)
                {
                    if (loadedProjects.Length > i)
                    {
                        rgbstrProjectNames[i] = loadedProjects[i];
                    }
                    else
                    {
                        pcProjectsFetched = (uint)i;
                        break;
                    }
                }
            }

            return VSConstants.S_OK;
        }

        int IVsSolution2.GetProjectFilesInSolution(uint grfGetOpts, uint cProjects, string[] rgbstrProjectNames, out uint pcProjectsFetched)
        {
            return ((IVsSolution)this).GetProjectFilesInSolution(grfGetOpts, cProjects, rgbstrProjectNames, out pcProjectsFetched);
        }

        int IVsSolution.GetProjectInfoOfProjref(string pszProjref, int propid, out object pvar)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.GetProjectInfoOfProjref(string pszProjref, int propid, out object pvar)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.GetProjectOfGuid(ref Guid rguidProjectID, out IVsHierarchy ppHierarchy)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.GetProjectOfGuid(ref Guid rguidProjectID, out IVsHierarchy ppHierarchy)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.GetProjectOfProjref(string pszProjref, out IVsHierarchy ppHierarchy, out string pbstrUpdatedProjref, VSUPDATEPROJREFREASON[] puprUpdateReason)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.GetProjectOfProjref(string pszProjref, out IVsHierarchy ppHierarchy, out string pbstrUpdatedProjref, VSUPDATEPROJREFREASON[] puprUpdateReason)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.GetProjectOfUniqueName(string pszUniqueName, out IVsHierarchy ppHierarchy)
        {
            ppHierarchy = this.Projects.Cast<Project>().Where(p => p.UniqueName == pszUniqueName).Cast<IVsHierarchy>().SingleOrDefault();
            return VSConstants.S_OK;
        }

        int IVsSolution2.GetProjectOfUniqueName(string pszUniqueName, out IVsHierarchy ppHierarchy)
        {
            return ((IVsSolution)this).GetProjectOfUniqueName(pszUniqueName, out ppHierarchy);
        }

        int IVsSolution.GetProjectTypeGuid(uint dwReserved, string pszMkProject, out Guid pguidProjectType)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.GetProjectTypeGuid(uint dwReserved, string pszMkProject, out Guid pguidProjectType)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.GetProjrefOfItem(IVsHierarchy pHierarchy, uint itemid, out string pbstrProjref)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.GetProjrefOfItem(IVsHierarchy pHierarchy, uint itemid, out string pbstrProjref)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.GetProjrefOfProject(IVsHierarchy pHierarchy, out string pbstrProjref)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.GetProjrefOfProject(IVsHierarchy pHierarchy, out string pbstrProjref)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.GetProperty(int propid, out object pvar)
        {
            pvar = null;
            if (propid == (int)__VSPROPID4.VSPROPID_IsSolutionFullyLoaded)
            {
                pvar = this.IsFullyLoaded;
                return VSConstants.S_OK;
            }

            return VSConstants.E_FAIL;
        }

        int IVsSolution2.GetProperty(int propid, out object pvar)
        {
            return ((IVsSolution)this).GetProperty(propid, out pvar);
        }

        int IVsSolution.GetSolutionInfo(out string pbstrSolutionDirectory, out string pbstrSolutionFile, out string pbstrUserOptsFile)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.GetSolutionInfo(out string pbstrSolutionDirectory, out string pbstrSolutionFile, out string pbstrUserOptsFile)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.GetUniqueNameOfProject(IVsHierarchy pHierarchy, out string pbstrUniqueName)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.GetUniqueNameOfProject(IVsHierarchy pHierarchy, out string pbstrUniqueName)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.GetVirtualProjectFlags(IVsHierarchy pHierarchy, out uint pgrfAddVPFlags)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.GetVirtualProjectFlags(IVsHierarchy pHierarchy, out uint pgrfAddVPFlags)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.OnAfterRenameProject(IVsProject pProject, string pszMkOldName, string pszMkNewName, uint dwReserved)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.OnAfterRenameProject(IVsProject pProject, string pszMkOldName, string pszMkNewName, uint dwReserved)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.OpenSolutionFile(uint grfOpenOpts, string pszFilename)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.OpenSolutionFile(uint grfOpenOpts, string pszFilename)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.OpenSolutionViaDlg(string pszStartDirectory, int fDefaultToAllProjectsFilter)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.OpenSolutionViaDlg(string pszStartDirectory, int fDefaultToAllProjectsFilter)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.QueryEditSolutionFile(out uint pdwEditResult)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.QueryEditSolutionFile(out uint pdwEditResult)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.QueryRenameProject(IVsProject pProject, string pszMkOldName, string pszMkNewName, uint dwReserved, out int pfRenameCanContinue)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.QueryRenameProject(IVsProject pProject, string pszMkOldName, string pszMkNewName, uint dwReserved, out int pfRenameCanContinue)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.RemoveVirtualProject(IVsHierarchy pHierarchy, uint grfRemoveVPFlags)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.RemoveVirtualProject(IVsHierarchy pHierarchy, uint grfRemoveVPFlags)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.SaveSolutionElement(uint grfSaveOpts, IVsHierarchy pHier, uint docCookie)
        {
            if (this.SaveSolutionElementAction == null)
            {
                throw new NotImplementedException();
            }

            return this.SaveSolutionElementAction(grfSaveOpts, pHier, docCookie);
        }

        int IVsSolution2.SaveSolutionElement(uint grfSaveOpts, IVsHierarchy pHier, uint docCookie)
        {
            return ((IVsSolution)this).SaveSolutionElement(grfSaveOpts, pHier, docCookie);
        }

        int IVsSolution.SetProperty(int propid, object var)
        {
            throw new NotImplementedException();
        }

        int IVsSolution2.SetProperty(int propid, object var)
        {
            throw new NotImplementedException();
        }

        int IVsSolution.UnadviseSolutionEvents(uint dwCookie)
        {
            dwCookie.Should().BeGreaterThan(0);
            dwCookie.Should().BeLessOrEqualTo((uint)this.sinks.Count);
            this.sinks.RemoveAt((int)dwCookie - 1);
            return VSConstants.S_OK;
        }

        int IVsSolution2.UnadviseSolutionEvents(uint dwCookie)
        {
            return ((IVsSolution)this).UnadviseSolutionEvents(dwCookie);
        }

        int IVsSolution2.UpdateProjectFileLocation(IVsHierarchy pHierarchy)
        {
            throw new NotImplementedException();
        }

        private class EnumHierarchies : EnumBase<ProjectMock, IVsHierarchy>, IEnumHierarchies
        {
            public EnumHierarchies(SolutionMock mock)
            {
                foreach (var project in mock.Projects)
                {
                    this.Items.Add(project);
                }
            }

            private EnumHierarchies(EnumHierarchies other)
                : base(other)
            {
            }

            protected override IVsHierarchy GetItem(ProjectMock input)
            {
                return input;
            }

            int IEnumHierarchies.Clone(out IEnumHierarchies ppenum)
            {
                ppenum = new EnumHierarchies(this);
                return VSConstants.S_OK;
            }

            int IEnumHierarchies.Next(uint celt, IVsHierarchy[] rgelt, out uint pceltFetched)
            {
                return this.Next(celt, rgelt, out pceltFetched);
            }

            int IEnumHierarchies.Reset()
            {
                return this.Reset();
            }

            int IEnumHierarchies.Skip(uint celt)
            {
                return this.Skip(celt);
            }
        }
        #endregion

        #region Test helpers
        public bool IsFullyLoaded
        {
            get; set;
        }

        public IEnumerable<ProjectMock> Projects
        {
            get
            {
                return this.projects.Values;
            }
        }

        public ProjectMock AddOrGetProject(string projectFile, bool isLoaded = true)
        {
            ProjectMock prj;
            if (!this.projects.TryGetValue(projectFile, out prj))
            {
                projects[projectFile] = prj = new ProjectMock(projectFile);
                prj.DTE = this.dte;
                this.SimulateProjectOpen(prj);

                if (isLoaded)
                {
                    this.SimulateProjectLoad(prj);
                }
            }
            return prj;
        }

        public void RemoveProject(ProjectMock project)
        {
            this.projects.Remove(project.FilePath);
            this.SimulateProjectClose(project);
            this.SimulateProjectUnload(project);
        }

        public void SimulateSolutionClose()
        {
            this.projects.Values.ToList().ForEach(p => this.RemoveProject(p));
            this.sinks.ForEach(s => s.OnAfterCloseSolution(this));
        }

        public void SimulateSolutionOpen()
        {
            this.sinks.ForEach(s => s.OnAfterOpenSolution(this, 0));
            this.projects.Values.ToList().ForEach(p => this.SimulateProjectOpen(p));
        }

        public void SimulateProjectOpen(ProjectMock project)
        {
            this.sinks.ForEach(s => s.OnAfterOpenProject(project, 0));
        }

        public void SimulateProjectClose(ProjectMock project)
        {
            this.sinks.ForEach(s => s.OnBeforeCloseProject(project, 0));
        }

        public void SimulateProjectLoad(ProjectMock project)
        {
            project.IsLoaded = true;
            this.sinks.ForEach(s => s.OnAfterLoadProject(project, null));
        }

        public void SimulateProjectUnload(ProjectMock project)
        {
            project.IsLoaded = false;
            this.sinks.ForEach(s => s.OnBeforeUnloadProject(project, null));
        }

        public Func<uint, IVsHierarchy, uint, int> SaveSolutionElementAction { get; set; }
        #endregion
    }
}
