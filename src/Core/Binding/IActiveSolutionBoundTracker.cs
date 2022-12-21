/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
        BindingConfiguration CurrentConfiguration { get; }

        /// <summary>
        /// Event to notify subscribers when the binding status of a solution have changed.
        /// This occurs when a new solution is opened, or the SonarQube binding status of the solution changes.
        /// The event is raised after the <see cref="ISonarQubeService"/> connection has finished
        /// opening/closing.
        /// </summary>
        event EventHandler<ActiveSolutionBindingEventArgs> SolutionBindingChanged;

        event EventHandler SolutionBindingUpdated;
    }

    /// <summary>
    /// Interface implemented by objects that need to be notified when the solution binding changes
    /// </summary>
    /// <remarks>The notification is handled by the <see cref="IActiveSolutionBoundTracker"/>.
    /// The objects will be notified *before* the <see cref="IActiveSolutionBoundTracker.SolutionBindingChanged"/>
    /// event is raised.
    /// It should be implemented by classes that cache solution-level binidng information.</remarks>
    public interface IBoundSolutionObserver
    {
        void OnSolutionBindingChanged();
    }

    public class ActiveSolutionBindingEventArgs : EventArgs
    {
        public ActiveSolutionBindingEventArgs(BindingConfiguration configuration)
        {
            Configuration = configuration;
        }

        public BindingConfiguration Configuration { get; }
    }
}
