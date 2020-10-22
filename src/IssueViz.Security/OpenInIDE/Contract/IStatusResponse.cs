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
    // TODO:
    // IdeName: should we include the VS version and/or edition?
    //  e.g. "Visual Studio", "Visual Studio 2015", "Visual Studio 2015 Community"

    // InstanceName: should this be a standalone value, or should the server concatenate it
    //  with the IdeName?

    // * Icon - format? size in pixels?

    public interface IStatusResponse
    {
        // TODO - up to the server team to decide if this field is required.
        /// <summary>
        /// Unique identifier for this instance of the IDE.
        /// </summary>
        /// <remarks>Allows the server to distinguish between responses from different IDEs.
        /// The format is unimportant, but the id must be unique across:
        /// * all instances and versions of the VS, and
        /// * all other instances of SonarLint in other IDEs.</remarks>
        string IdeInstanceId { get; }

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
        /// This string should be the same for all instances of the current IDE flavour.
        /// Will be displayed by the server in the UI e.g. in a menu caption / tooltip
        /// </remarks>
        string InstanceName { get; }

        // TODO - byte array or encoded string?
        /// <summary>
        /// The icon to display for this IDE type
        /// </summary>
        byte[] Icon { get; }
    }
}
