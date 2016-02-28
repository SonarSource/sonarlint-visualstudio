//-----------------------------------------------------------------------
// <copyright file="ProjectSystemHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SonarLint.VisualStudio.Integration
{
    internal class ProjectSystemHelper : IProjectSystemHelper
    {
        internal const string VbProjectKind = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";
        internal const string CSharpProjectKind = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";

        // This constant is necessary to find the name of the "Solution Items" folder
        // for the CurrentUICulture. They correspond to a resource string in the satellite dll
        // for the msenv.dll package. The ID is the resource ID, and the guid is the package guid.
        internal const uint SolutionItemResourceId = 13450;

        private readonly IServiceProvider serviceProvider;

        public ProjectSystemHelper(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider
        {
            get { return this.serviceProvider; }
        }

        public IEnumerable<Project> GetSolutionManagedProjects()
        {
            IVsSolution solution = this.serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            Debug.Assert(solution != null, "Cannot find SVsSolution");

            return EnumerateProjects(solution)
                .Select(h =>
                    {
                        Debug.Assert(h != null);
                        object project = null;
                        if (ErrorHandler.Succeeded(h.GetProperty((uint)VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out project)))
                        {
                            return project as Project;
                        }
                        return null;
                    })
                .Where(p => p != null && IsManagedProject(p));
        }


        public bool IsFileInProject(Project project, string file)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }
            if (string.IsNullOrWhiteSpace(file))
            {
                throw new ArgumentNullException(nameof(file));
            }

            IVsSolution solution = this.serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            Debug.Assert(solution != null, "Cannot find SVsSolution");

            IVsHierarchy projectHierarchy;
            if (ErrorHandler.Succeeded(solution.GetProjectOfUniqueName(project.UniqueName, out projectHierarchy)))
            {
                IVsProject vsProject = projectHierarchy as IVsProject;
                int pfFound;
                VSDOCUMENTPRIORITY[] pdwPriority = new VSDOCUMENTPRIORITY[1];
                uint itemId;
                if (ErrorHandler.Succeeded(vsProject.IsDocumentInProject(file, out pfFound, pdwPriority, out itemId)) && pfFound != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public void AddFileToProject(Project project, string fullFilePath)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (string.IsNullOrWhiteSpace(fullFilePath))
            {
                throw new ArgumentNullException(nameof(fullFilePath));
            }

            if (!this.IsFileInProject(project, fullFilePath))
            {
                project.ProjectItems.AddFromFile(fullFilePath);
            }
        }

        public Solution2 GetCurrentActiveSolution()
        {
            DTE dte = this.serviceProvider.GetService(typeof(DTE)) as DTE;
            Debug.Assert(dte != null, "Could not find the DTE service");

            Solution2 solution = (Solution2)dte?.Solution;

            return solution;
        }

        public Project GetSolutionItemsProject()
        {
            string solutionItemsFolderName = GetSolutionItemsFolderName(this.serviceProvider);

            Solution2 solution = this.GetCurrentActiveSolution();

            Project solutionItemsProject = null;
            // Enumerating instead of using OfType<Project> due to a bug in
            // install shield projects that will throw an exception
            foreach (Project project in solution.Projects)
            {
                // Check if Solution Items folder already exists
                if (project.Name == solutionItemsFolderName)
                {
                    solutionItemsProject = project;
                    break;
                }
            }

            // Create Solution Items folder if it does not exist
            if (solutionItemsProject == null)
            {
                solutionItemsProject = solution.AddSolutionFolder(solutionItemsFolderName);
            }

            return solutionItemsProject;
        }

        private static string GetSolutionItemsFolderName(IServiceProvider serviceProvider)
        {
            string solutionItemsFolderName = null;
            Guid guid = VSConstants.CLSID.VsEnvironmentPackage_guid;

            IVsShell shell = serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
            Debug.Assert(shell != null, "Could not find the SVsShell service");

            ErrorHandler.ThrowOnFailure(shell.LoadPackageString(ref guid, SolutionItemResourceId, out solutionItemsFolderName));
            Debug.Assert(!string.IsNullOrEmpty(solutionItemsFolderName));
            return solutionItemsFolderName;
        }

        private static IEnumerable<IVsHierarchy> EnumerateProjects(IVsSolution solution)
        {
            Guid empty = Guid.Empty;
            IEnumHierarchies projectsEnum;
            ErrorHandler.ThrowOnFailure(solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, ref empty, out projectsEnum));
            IVsHierarchy[] output = new IVsHierarchy[1];
            uint fetched;
            while (ErrorHandler.Succeeded(projectsEnum.Next(1, output, out fetched)) && fetched == 1)
            {
                yield return output[0];
            }
        }

        private static bool IsManagedProject(Project project)
        {
            return IsCSharpProject(project) || IsVBProject(project);
        }

        public static bool IsVBProject(Project project)
        {
            return IsProjectKind(project, VbProjectKind);
        }

        public static bool IsCSharpProject(Project project)
        {
            return IsProjectKind(project, CSharpProjectKind);
        }

        private static bool IsProjectKind(Project project, string projectKindGuidString)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(projectKindGuidString, project.Kind);
        }
    }
}
