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

using System.Collections.Generic;
using System.Linq;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSolutionBindingInformationProvider : ISolutionBindingInformationProvider
    {
        public IEnumerable<Project> BoundProjects { get; set; } = Enumerable.Empty<Project>();

        public IEnumerable<Project> UnboundProjects { get; set; } = Enumerable.Empty<Project>();

        public ActiveSolutionBinding ActiveSolutionBinding { get; set; } = new ActiveSolutionBinding();

        public IEnumerable<Project> GetBoundProjects()
        {
            return this.BoundProjects;
        }

        public IEnumerable<Project> GetUnboundProjects()
        {
            return this.UnboundProjects;
        }

        public ActiveSolutionBinding GetActiveSolutionBinding()
        {
            return ActiveSolutionBinding;
        }
    }
}