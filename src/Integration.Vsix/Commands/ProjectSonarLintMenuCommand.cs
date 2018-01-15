/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Command handler for the SonarLint menu.
    /// </summary>
    /// <remarks>Has no invocation logic; only has QueryStatus</remarks>
    internal class ProjectSonarLintMenuCommand : VsCommandBase
    {
        private readonly IProjectPropertyManager propertyManager;

        public ProjectSonarLintMenuCommand(IProjectPropertyManager propertyManager)
        {
            if (propertyManager == null)
            {
                throw new ArgumentNullException(nameof(propertyManager));
            }

            this.propertyManager = propertyManager;
        }

        protected override void InvokeInternal()
        {
            // Do nothing; this is a menu only.
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            Debug.Assert(this.propertyManager != null, "Property manager should not be null");
            command.Enabled = false;
            command.Visible = false;

            IList<Project> projects = this.propertyManager
                                          .GetSelectedProjects()
                                          .ToList();

            if (projects.Any() && projects.All(x => Language.ForProject(x).IsSupported))
            {
                command.Enabled = true;
                command.Visible = true;
            }
        }
    }
}
