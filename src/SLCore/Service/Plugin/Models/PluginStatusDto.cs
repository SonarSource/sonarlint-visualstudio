/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

/// <param name="pluginName">human-readable name of the language/analyzer (e.g. "Java", "C/C++/Objective-C")</param>
/// <param name="state">current lifecycle state of the plugin in the backend</param>
/// <param name="source">where the plugin artifact came from; null when the plugin is not available</param>
/// <param name="actualVersion">version of the plugin that is currently in use; null when the plugin is not loaded</param>
/// <param name="overriddenVersion">a local plugin version that was superseded by the one obtained via SQS/SQC sync, if any; null when no override is in effect</param>
public record PluginStatusDto(string pluginName, PluginStateDto state, ArtifactSourceDto? source, string? actualVersion, string? overriddenVersion);
