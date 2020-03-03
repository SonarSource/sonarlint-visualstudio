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

namespace SonarLint.VisualStudio.Integration
{
    internal class TestProjectIndicator : ITestProjectIndicator
    {
        private readonly ITestProjectIndicator buildPropertyIndicator;
        private readonly ITestProjectIndicator projectKindIndicator;
        private readonly ITestProjectIndicator projectNameIndicator;

        public TestProjectIndicator(ITestProjectIndicator buildPropertyIndicator,
            ITestProjectIndicator projectKindIndicator,
            ITestProjectIndicator projectNameIndicator)
        {
            this.buildPropertyIndicator = buildPropertyIndicator ?? throw new ArgumentNullException(nameof(buildPropertyIndicator));
            this.projectKindIndicator = projectKindIndicator ?? throw new ArgumentNullException(nameof(projectKindIndicator));
            this.projectNameIndicator = projectNameIndicator ?? throw new ArgumentNullException(nameof(projectNameIndicator));
        }

        public bool? IsTestProject(EnvDTE.Project project)
        {
            var isTestProject = buildPropertyIndicator.IsTestProject(project);

            if (isTestProject.HasValue)
            {
                return isTestProject.Value;
            }

            return projectKindIndicator.IsTestProject(project).GetValueOrDefault(false) ||
                   projectNameIndicator.IsTestProject(project).GetValueOrDefault(false);
        }
    }
}
