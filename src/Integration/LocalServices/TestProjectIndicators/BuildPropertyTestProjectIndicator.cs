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
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration
{
    internal class BuildPropertyTestProjectIndicator : ITestProjectIndicator
    {
        private readonly IProjectSystemHelper projectSystem;
        private readonly IProjectPropertyManager propertyManager;

        public BuildPropertyTestProjectIndicator(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            projectSystem = serviceProvider.GetService<IProjectSystemHelper>();
            projectSystem.AssertLocalServiceIsNotNull();

            propertyManager = serviceProvider.GetMefService<IProjectPropertyManager>();
            Debug.Assert(propertyManager != null, $"Failed to get {nameof(IProjectPropertyManager)}");
        }

        public bool? IsTestProject(Project project)
        {
            VerifyThatProjectHasBuildProperties(project);

            var sonarTest = propertyManager.GetBooleanProperty(project, Constants.SonarQubeTestProjectBuildPropertyKey);

            return sonarTest;
        }

        private void VerifyThatProjectHasBuildProperties(Project project)
        {
            var hierarchy = projectSystem.GetIVsHierarchy(project);

            if (!(hierarchy is IVsBuildPropertyStorage))
            {
                throw new ArgumentException(Strings.ProjectFilterDteProjectFailedToGetIVs, nameof(project));
            }
        }
    }
}
