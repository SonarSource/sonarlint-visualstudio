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

namespace SonarLint.VisualStudio.Core
{
    public interface IToolWindowService
    {
        /// <summary>
        /// Opens specified tool window, brings it to the front and gives it focus
        /// </summary>
        /// <remarks>The window will be created if it does not already exist</remarks>
        void Show(Guid toolWindowId);

        /// <summary>
        /// Creates the window if it does not already exist
        /// </summary>
        /// <remarks>If a new tool window is created it will not be brought to the front or given focus.
        /// If the tool window already exists its visibility and focus will not be affected.</remarks>
        void EnsureToolWindowExists(Guid toolWindowId);
    }
}
