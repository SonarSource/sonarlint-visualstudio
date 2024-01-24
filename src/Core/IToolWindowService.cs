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
        /// Creates the window and its content if it does not already exist
        /// </summary>
        /// <remarks>If a new tool window is created it will not be brought to the front or given focus.
        /// If the tool window already exists its visibility and focus will not be affected.
        /// <para>
        /// Note: VS will delay creating a window until it actually needs to render it. For example, if a
        /// tool window is docked but not top-most then VS can create a tab that displays the tool window
        /// caption without actually creating the tool window content (e.g. when showing a window based on
        /// a UIContext). This method ensures the window content is created.
        /// </para>
        /// </remarks>
        void EnsureToolWindowExists(Guid toolWindowId);

        /// <summary>
        /// Returns a tool window of type V, or null if one could not be found.
        /// </summary
        /// <typeparam name="T">The type of tool window. Should be a subclass of <see cref="Microsoft.VisualStudio.Shell.ToolWindowPane"/></typeparam>
        V GetToolWindow<T, V>() where T : class;
    }
}
