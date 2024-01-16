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

namespace SonarLint.VisualStudio.Core
{
    // Note: this is a hack. We shouldn't need this interface: it only exists to avoid a
    // circular import reference between the ActiveSolutionBoundTracker and the
    // BoundSolutionGitMonitor classes.

    /// <summary>
    /// Raises events for git changes to bound solutions
    /// </summary>
    /// <remarks>The implementation is a singleton, and can handle switching to monitor
    /// different repos as solutions are opened and closed</remarks>
    public interface IBoundSolutionGitMonitor : IGitEvents
    {
        /// <summary>
        /// Tells the instance that it needs to recalculate the repo to monitor
        /// </summary>
        void Refresh();
    }
}
