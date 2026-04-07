/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

namespace SonarLint.VisualStudio.SLCore.Service.Plugin.Models;

/// <summary>
/// Represents the current state of an analyzer plugin as observed by the backend.
/// </summary>
public enum PluginStateDto
{
    /// <summary>
    /// The plugin is loaded and ready for analysis.
    /// </summary>
    ACTIVE,

    /// <summary>
    /// The plugin was downloaded from a SonarQube Server or SonarQube Cloud connection.
    /// </summary>
    SYNCED,

    /// <summary>
    /// The plugin is currently being downloaded.
    /// </summary>
    DOWNLOADING,

    /// <summary>
    /// The plugin failed to load or is otherwise unavailable.
    /// </summary>
    FAILED,

    /// <summary>
    /// The plugin is available only in connected mode (premium feature).
    /// </summary>
    PREMIUM,

    /// <summary>
    /// The plugin is not supported in the current IDE or platform.
    /// </summary>
    UNSUPPORTED
}
