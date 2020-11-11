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

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Contract
{


    public interface IStatusResponse
    {
        /// <summary>
        /// Short user-friendly name for the current IDE flavour.
        /// </summary
        /// <remarks>
        /// This string should be the same for all instances of the current IDE flavour.
        /// Will be displayed by the server in the UI e.g. in a menu caption / tooltip
        /// </remarks>
        string IdeName { get; }

        /// <summary>
        /// Longer user-friendly name containing information to disambiguate this IDE instance
        /// from other running instances.
        /// </summary
        /// <remarks>
        /// This string should be unique to the current VS instance
        /// Will be displayed by the server in the UI e.g. in a menu caption / tooltip
        /// </remarks>
        string Description { get; }
    }
}
