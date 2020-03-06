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
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Resources;
using DteProject = EnvDTE.Project;

namespace SonarLint.VisualStudio.Integration
{
    internal class ProjectSystemFilter : IProjectSystemFilter
    {
        private readonly ITestProjectIndicator testProjectIndicator;
        private readonly IProjectSystemHelper projectSystem;
        private readonly IProjectPropertyManager propertyManager;

        public ProjectSystemFilter(IHost host, ITestProjectIndicator testProjectIndicator)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.testProjectIndicator = testProjectIndicator ?? throw new ArgumentNullException(nameof(testProjectIndicator));

            this.projectSystem = host.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();

            this.propertyManager = host.GetMefService<IProjectPropertyManager>();
            Debug.Assert(this.propertyManager != null, $"Failed to get {nameof(IProjectPropertyManager)}");
        }

        #region IProjectSystemFilter

        public bool IsAccepted(DteProject project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var hierarchy = this.projectSystem.GetIVsHierarchy(project);
            var propertyStorage = hierarchy as IVsBuildPropertyStorage;

            if (hierarchy == null || propertyStorage == null)
            {
                throw new ArgumentException(Strings.ProjectFilterDteProjectFailedToGetIVs, nameof(project));
            }

            var isUnsupported = IsNotSupportedProject(project) ||
                                IsSharedProject(project) ||
                                IsExcludedViaProjectProperty(project) ||
                                testProjectIndicator.IsTestProject(project).GetValueOrDefault(false);

            return !isUnsupported;
        }

        #endregion

        #region Helpers

        private static bool IsNotSupportedProject(DteProject project)
        {
            var language = ProjectToLanguageMapper.GetLanguageForProject(project);
            return (language == null || !language.IsSupported);
        }

        private bool IsExcludedViaProjectProperty(DteProject dteProject)
        {
            Debug.Assert(dteProject != null);

            // General exclusions
            // If exclusion property is set to true, this takes precedence
            bool? sonarExclude = this.propertyManager.GetBooleanProperty(dteProject, Constants.SonarQubeExcludeBuildPropertyKey);
            return sonarExclude.HasValue && sonarExclude.Value;
        }

        private static bool IsSharedProject(DteProject project) =>
            // Note: VB and C# shared projects don't have a project property that identifies them as a shared project.
            // They do have common imports that we could search for, but we'll rely on the fact that they both have
            // ".shproj" file extensions.
            // C++ shared projects use a different file extension, but we don't currently support C++ projects in
            // connected mode.
            System.IO.Path.GetExtension(project.FileName).Equals(".shproj", StringComparison.OrdinalIgnoreCase);

        #endregion
    }
}
