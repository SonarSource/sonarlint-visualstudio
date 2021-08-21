/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using DteProject = EnvDTE.Project;

namespace SonarLint.VisualStudio.Integration
{
    internal class ProjectSystemFilter : IProjectSystemFilter
    {
        private readonly ITestProjectIndicator testProjectIndicator;
        private readonly IProjectPropertyManager propertyManager;
        private readonly IProjectToLanguageMapper projectToLanguageMapper;

        public ProjectSystemFilter(IHost host, ITestProjectIndicator testProjectIndicator)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.testProjectIndicator = testProjectIndicator ?? throw new ArgumentNullException(nameof(testProjectIndicator));

            this.propertyManager = host.GetMefService<IProjectPropertyManager>();
            Debug.Assert(this.propertyManager != null, $"Failed to get {nameof(IProjectPropertyManager)}");

            projectToLanguageMapper = host.GetMefService<IProjectToLanguageMapper>();
        }

        #region IProjectSystemFilter

        public bool IsAccepted(DteProject project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }
            var isUnsupported = IsNotSupportedProject(project) ||
                                IsSharedProject(project) ||
                                IsExcludedViaProjectProperty(project) ||
                                testProjectIndicator.IsTestProject(project).GetValueOrDefault(false);

            return !isUnsupported;
        }

        #endregion

        #region Helpers

        private bool IsNotSupportedProject(DteProject project)
        {
            return !projectToLanguageMapper.HasSupportedLanguage(project);
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
