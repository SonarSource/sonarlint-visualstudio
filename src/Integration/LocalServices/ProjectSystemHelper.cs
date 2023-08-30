/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IVsHierarchyLocator))]
    [Export(typeof(IProjectSystemHelper))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ProjectSystemHelper : IProjectSystemHelper
    {
        // See https://github.com/dotnet/project-system/blob/master/docs/opening-with-new-project-system.md
        internal const string VbProjectKind = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";
        internal const string CSharpProjectKind = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
        internal const string VbCoreProjectKind = "{778DAE3C-4631-46EA-AA77-85C1314464D9}";
        internal const string CSharpCoreProjectKind = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";
        internal const string CppProjectKind = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}";
        internal const string NodeJSProjectKind = "{9092aa53-fb77-4645-b42d-1ccca6bd08bd}";

        internal const string TestProjectKind = "{3AC096D0-A1C2-E12C-1390-A8335801FDAB}";
        internal static readonly Guid TestProjectKindGuid = new Guid(TestProjectKind);
        internal const string VsProjectItemKindSolutionFolder = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";

        /// <summary>
        /// This is the HResult returned by IVsBuildPropertyStorage when attempting to
        /// read a property that does not exist.
        /// </summary>
        public const int E_XML_ATTRIBUTE_NOT_FOUND = unchecked((int)0x8004C738);

        private readonly IServiceProvider serviceProvider;
        private readonly IProjectToLanguageMapper projectToLanguageMapper;

        [ImportingConstructor]
        public ProjectSystemHelper([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IProjectToLanguageMapper projectToLanguageMapper)
        {
            this.serviceProvider = serviceProvider;
            this.projectToLanguageMapper = projectToLanguageMapper;
        }

        public IVsHierarchy GetVsHierarchyForFile(string fileName)
        {
            var dte = serviceProvider.GetService<SDTE, DTE2>();
            var projectItem = dte?.Solution?.FindProjectItem(fileName);

            return projectItem?.ContainingProject == null ? null : GetIVsHierarchy(projectItem.ContainingProject);
        }

        public IEnumerable<Project> GetSolutionProjects()
        {
            IVsSolution solution = this.serviceProvider.GetService<SVsSolution, IVsSolution>();
            Debug.Assert(solution != null, "Cannot find SVsSolution");

            foreach (var hierarchy in EnumerateProjects(solution))
            {
                Project Project = GetProject(hierarchy);

                if (Project != null && 
                    projectToLanguageMapper.HasSupportedLanguage(Project))
                {
                    yield return Project;
                }
            }
        }

        public Project GetProject(IVsHierarchy projectHierarchy)
        {
            if (projectHierarchy == null)
            {
                throw new ArgumentNullException(nameof(projectHierarchy));
            }

            object project;
            if (ErrorHandler.Succeeded(projectHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out project)))
            {
                return project as Project;
            }

            return null;
        }

        public IVsHierarchy GetIVsHierarchy(Project dteProject)
        {
            if (dteProject == null)
            {
                throw new ArgumentNullException(nameof(dteProject));
            }

            IVsSolution solution = this.serviceProvider.GetService<SVsSolution, IVsSolution>();
            Debug.Assert(solution != null, "Cannot find SVsSolution");

            IVsHierarchy hierarchy;
            if (ErrorHandler.Succeeded(solution.GetProjectOfUniqueName(dteProject.UniqueName, out hierarchy)))
            {
                return hierarchy;
            }

            return null;
        }

        public IEnumerable<Project> GetSelectedProjects()
        {
            var dte = serviceProvider.GetService<SDTE, DTE2>();
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
            return GetProjectProperty(dteProject, propertyName, string.Empty);
        }

        public string GetProjectProperty(Project dteProject, string propertyName, string configuration)
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
                var hr = propertyStorage.GetPropertyValue(propertyName, configuration,
                    (uint)_PersistStorageType.PST_PROJECT_FILE, out value);

                // E_XML_ATTRIBUTE_NOT_FOUND is returned when the property does not exist - this is OK.
                Debug.Assert(!ErrorHandler.Succeeded(hr) || hr != E_XML_ATTRIBUTE_NOT_FOUND,
                    $"Failed to get the property '{propertyName}' for project '{dteProject.Name}'.");
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

            return GetAggregateProjectKindsIterator(hierarchy);
        }

        private IEnumerable<Guid> GetAggregateProjectKindsIterator(IVsHierarchy hierarchy)
        {
            // TODO: is this relevant for core projects?

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
    }
}
