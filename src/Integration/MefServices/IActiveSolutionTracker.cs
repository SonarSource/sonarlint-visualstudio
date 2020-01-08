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
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration
{
    public class ActiveSolutionChangedEventArgs : EventArgs
    {
        public ActiveSolutionChangedEventArgs(bool isSolutionOpen)
        {
            IsSolutionOpen = isSolutionOpen;
        }

        public bool IsSolutionOpen { get; }
    }

    public class ProjectOpenedEventArgs : EventArgs
    {
        public ProjectOpenedEventArgs(IVsHierarchy pHierarchy)
        {
            ProjectHierarchy = pHierarchy;
        }

        public IVsHierarchy ProjectHierarchy { get; }
    }

    public interface IActiveSolutionTracker
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
