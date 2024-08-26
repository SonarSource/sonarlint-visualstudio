/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

namespace SonarLint.VisualStudio.Core.Binding
{
    /// <summary>
    /// Allows checking if the current Visual Studio solution is bound to a SonarQube project or not
    /// </summary>
    public interface IActiveSolutionBoundTracker
    {
        /// <summary>
        /// Get the current binding configuration.
        /// </summary>
        /// <remarks>
        /// This is never null.
        /// </remarks>
        BindingConfiguration2 CurrentConfiguration { get; }

        /// <summary>
        /// Raised after the binding has been updated, and before the <see cref="SolutionBindingUpdated"/> event
        /// </summary>
        /// <remarks>Most listeners should use the <see cref="SolutionBindingChanged"> event.
        /// This event is intended for components that need to recalculate/refresh data that
        /// is then used by listeners that consume the <see cref="SolutionBindingChanged"/> event
        /// e.g. components that cache solution-level data.</remarks>
        event EventHandler<ActiveSolutionBindingEventArgs> PreSolutionBindingChanged;

        /// <summary>
        /// Event to notify subscribers when the binding status of a solution have changed.
        /// This occurs when a new solution is opened, or the SonarQube binding status of the solution changes.
        /// The event is raised after the <see cref="ISonarQubeService"/> connection has finished
        /// opening/closing.
        /// </summary>
        event EventHandler<ActiveSolutionBindingEventArgs> SolutionBindingChanged;

        /// <summary>
        /// Raised when an existing binding has been updated, and before the <see cref="SolutionBindingUpdated"/> event
        /// </summary>
        event EventHandler PreSolutionBindingUpdated;

        /// <summary>
        /// Raised when an existing binding has been updated i.e. the solution is still bound to the same
        /// Sonar project (e.g. the user updated the binding via the Team Explorer)
        /// </summary>
        event EventHandler SolutionBindingUpdated;
    }

    public class ActiveSolutionBindingEventArgs : EventArgs
    {
        public ActiveSolutionBindingEventArgs(BindingConfiguration2 configuration)
        {
            Configuration = configuration;
        }

        public BindingConfiguration2 Configuration { get; }
    }
}
