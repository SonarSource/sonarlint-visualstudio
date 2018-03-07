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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration
{
    internal class ActiveSolutionChangedEventArgs : EventArgs
    {
        public ActiveSolutionChangedEventArgs(bool isSolutionOpen)
        {
            IsSolutionOpen = isSolutionOpen;
        }

        public bool IsSolutionOpen { get; }
    }

    internal class ProjectOpenedEventArgs : EventArgs
    {
        private readonly IVsHierarchy pHierarchy;
        private readonly Lazy<EnvDTE.Project> project;

        public ProjectOpenedEventArgs(IVsHierarchy pHierarchy)
        {
            this.pHierarchy = pHierarchy;
            this.project = new Lazy<EnvDTE.Project>(GetProject);
        }

        public EnvDTE.Project Project => project.Value;

        private EnvDTE.Project GetProject()
        {
            object objProj;
            pHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out objProj);

            return objProj as EnvDTE.Project;
        }
    }

    internal interface IActiveSolutionTracker
    {
        /// <summary>
        /// The active solution has changed (either opened or closed).
        /// </summary>
        /// <remarks>The solution might not be fully loaded when this event is raised.
        /// The event argument value will be true if a solution is open and false otherwise.</remarks>
        event EventHandler<ActiveSolutionChangedEventArgs> ActiveSolutionChanged;

        event EventHandler<ProjectOpenedEventArgs> AfterProjectOpened;
    }
}
