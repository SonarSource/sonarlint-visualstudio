/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SonarLint.VisualStudio.Integration
{
    internal class ProjectSystemHelper : IProjectSystemHelper
    {
        internal const string VbProjectKind = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";
        internal const string CSharpProjectKind = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
        internal const string TestProjectKind = "{3AC096D0-A1C2-E12C-1390-A8335801FDAB}";
        internal static readonly Guid TestProjectKindGuid = new Guid(TestProjectKind);
        internal const string VsProjectItemKindSolutionFolder = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";

        /// <summary>
        /// This is the HResult returned by IVsBuildPropertyStorage when attempting to
        /// read a property that does not exist.
        /// </summary>
        public const int E_XML_ATTRIBUTE_NOT_FOUND = unchecked((int)0x8004C738);

        // This constant is necessary to find the name of the "Solution Items" folder
        // for the CurrentUICulture. They correspond to a resource string in the satellite DLL
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

        public IEnumerable<Project> GetSolutionProjects()
        {
            IVsSolution solution = this.serviceProvider.GetService<SVsSolution, IVsSolution>();
            Debug.Assert(solution != null, "Cannot find SVsSolution");

            foreach (var hierarchy in EnumerateProjects(solution))
            {
                Project Project = GetProject(hierarchy);
                if (Project != null && !Language.ForProject(Project).Equals(Language.Unknown))
                {
                    yield return Project;
                }
            }
        }

        public IEnumerable<Project> GetFilteredSolutionProjects()
        {
            var projectFilter = this.serviceProvider.GetService<IProjectSystemFilter>();
            projectFilter.AssertLocalServiceIsNotNull();

            return GetSolutionProjects().Where(x => projectFilter.IsAccepted(x));
        }

        private static Project GetProject(IVsHierarchy hierarchy)
        {
            if (hierarchy == null)
            {
                throw new ArgumentNullException(nameof(hierarchy));
            }

            object project;
            if (ErrorHandler.Succeeded(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out project)))
            {
                return project as Project;
            }

            return null;
        }

        public IVsHierarchy GetIVsHierarchy(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            IVsSolution solution = this.serviceProvider.GetService<SVsSolution, IVsSolution>();
            Debug.Assert(solution != null, "Cannot find SVsSolution");

            IVsHierarchy hierarchy;
            if (ErrorHandler.Succeeded(solution.GetProjectOfUniqueName(project.UniqueName, out hierarchy)))
            {
                return hierarchy;
            }

            return null;
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

        public void AddFileToProject(Project project, string fullFilePath, string itemType)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (string.IsNullOrWhiteSpace(fullFilePath))
            {
                throw new ArgumentNullException(nameof(fullFilePath));
            }

            if (string.IsNullOrWhiteSpace(itemType))
            {
                throw new ArgumentNullException(nameof(itemType));
            }

            if (!this.IsFileInProject(project, fullFilePath))
            {
                ProjectItem item = project.ProjectItems.AddFromFile(fullFilePath);
                Property itemTypeProperty = VsShellUtils.FindProperty(item.Properties, Constants.ItemTypePropertyKey);
                if (itemTypeProperty != null)
                {
                    itemTypeProperty.Value = itemType;
                }
                else
                {
                    Debug.Fail("Failed to set the ItemType of the project item");
                }
            }
        }

        public void RemoveFileFromProject(Project project, string fileName)
        {
            foreach (ProjectItem projectItem in project.ProjectItems)
            {
                if (projectItem.Name == fileName)
                {
                    projectItem.Remove(); // We only want to remove from project not from disk
                    break;
                }
            }

            // If the project is an empty solution folder, then remove it
            if (project.ProjectItems.Count == 0 && project.Kind == ProjectSystemHelper.VsProjectItemKindSolutionFolder)
            {
                Solution2 solution = this.GetCurrentActiveSolution();
                solution.Remove(project);
            }
        }

        public Solution2 GetCurrentActiveSolution()
        {
            DTE dte = this.serviceProvider.GetService(typeof(DTE)) as DTE;
            Debug.Assert(dte != null, "Could not find the DTE service");

            Solution2 solution = (Solution2)dte?.Solution;

            return solution;
        }

        public Project GetSolutionItemsProject(bool createOnNull)
        {
            string solutionItemsFolderName = this.GetSolutionItemsFolderName();

            return this.GetSolutionFolderProject(solutionItemsFolderName, createOnNull);
        }

        public Project GetSolutionFolderProject(string solutionFolderName, bool createOnNull)
        {
            Solution2 solution = this.GetCurrentActiveSolution();

            Project solutionItemsProject = null;
            // Enumerating instead of using OfType<Project> due to a bug in
            // install shield projects that will throw an exception
            foreach (Project project in solution.Projects)
            {
                // Check if SonarQube solution folder already exists
                if (project.Name == solutionFolderName
                    && project.Kind == ProjectSystemHelper.VsProjectItemKindSolutionFolder)
                {
                    solutionItemsProject = project;
                    break;
                }
            }

            // Create Solution Items folder if it does not exist
            if (solutionItemsProject == null && createOnNull)
            {
                solutionItemsProject = solution.AddSolutionFolder(solutionFolderName);
            }

            return solutionItemsProject;
        }

        public IEnumerable<Project> GetSelectedProjects()
        {
            var dte = this.serviceProvider.GetService<DTE>();
            if (dte == null)
            {
                Debug.Fail("Failed to get DTE");
                yield break;
            }

            foreach (object projectObj in dte.ActiveSolutionProjects as Array ?? new object[0])
            {
                var project = projectObj as Project;
                if (project != null)
                {
                    yield return project;
                }
            }
        }

        public string GetProjectProperty(Project dteProject, string propertyName)
        {
            if (dteProject == null)
            {
                throw new ArgumentNullException(nameof(dteProject));
            }

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            string value = null;
            IVsHierarchy projectHierarchy = this.GetIVsHierarchy(dteProject);
            IVsBuildPropertyStorage propertyStorage = projectHierarchy as IVsBuildPropertyStorage;

            if (propertyStorage != null)
            {
                var hr = propertyStorage.GetPropertyValue(propertyName, string.Empty,
                    (uint)_PersistStorageType.PST_PROJECT_FILE, out value);

                // E_XML_ATTRIBUTE_NOT_FOUND is returned when the property does not exist - this is OK.
                Debug.Assert(!ErrorHandler.Succeeded(hr) || hr != E_XML_ATTRIBUTE_NOT_FOUND,
                    $"Failed to get the property '{propertyName}' for project '{dteProject.Name}'.");
            }
            else
            {
                Debug.Fail("Could not get IVsBuildPropertyStorage for EnvDTE.Project");
            }

            return value;
        }

        public void SetProjectProperty(Project dteProject, string propertyName, string value)
        {
            if (dteProject == null)
            {
                throw new ArgumentNullException(nameof(dteProject));
            }

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            IVsHierarchy projectHierarchy = this.GetIVsHierarchy(dteProject);
            IVsBuildPropertyStorage propertyStorage = projectHierarchy as IVsBuildPropertyStorage;
            if (propertyStorage != null)
            {
                var hr = propertyStorage.SetPropertyValue(propertyName, string.Empty,
                    (uint)_PersistStorageType.PST_PROJECT_FILE, value);

                Debug.Assert(ErrorHandler.Succeeded(hr), $"Failed to set property '{propertyName}' to '{value}' for project '{dteProject.Name}'.");
            }
            else
            {
                Debug.Fail("Could not get IVsBuildPropertyStorage for EnvDTE.Project");
            }
        }

        public void ClearProjectProperty(Project dteProject, string propertyName)
        {
            if (dteProject == null)
            {
                throw new ArgumentNullException(nameof(dteProject));
            }

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            IVsHierarchy projectHierarchy = this.GetIVsHierarchy(dteProject);
            IVsBuildPropertyStorage propertyStorage = projectHierarchy as IVsBuildPropertyStorage;
            if (propertyStorage != null)
            {
                var hr = propertyStorage.RemoveProperty(propertyName, string.Empty,
                    (uint)_PersistStorageType.PST_PROJECT_FILE);

                Debug.Assert(ErrorHandler.Succeeded(hr), $"Failed to remove property '{propertyName}' for project '{dteProject.Name}'.");
            }
            else
            {
                Debug.Fail("Could not get IVsBuildPropertyStorage for EnvDTE.Project");
            }
        }

        public IEnumerable<Guid> GetAggregateProjectKinds(IVsHierarchy hierarchy)
        {
            if (hierarchy == null)
            {
                throw new ArgumentNullException(nameof(hierarchy));
            }

            IVsAggregatableProjectCorrected aggregatableProject = hierarchy as IVsAggregatableProjectCorrected;
            if (aggregatableProject == null)
            {
                yield break;
            }

            string guidStrings;
            if (ErrorHandler.Succeeded(aggregatableProject.GetAggregateProjectTypeGuids(out guidStrings)))
            {
                foreach (var guidStr in guidStrings.Split(';'))
                {
                    Guid guid;
                    if (Guid.TryParse(guidStr, out guid))
                    {
                        yield return guid;
                    }
                }
            }
        }

        private string GetSolutionItemsFolderName()
        {
            string solutionItemsFolderName;
            Guid guid = VSConstants.CLSID.VsEnvironmentPackage_guid;

            IVsShell shell = this.serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
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
