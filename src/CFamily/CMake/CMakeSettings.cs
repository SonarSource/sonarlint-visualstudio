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

using Newtonsoft.Json;

namespace SonarLint.VisualStudio.CFamily.CMake
{
    /// <summary>
    /// Schema based on https://docs.microsoft.com/en-us/cpp/build/cmakesettings-reference?view=msvc-160
    /// </summary>
    internal class CMakeSettings
    {
        [JsonProperty("configurations")]
        public CMakeBuildConfiguration[] Configurations { get; set; }
    }

    internal class CMakeBuildConfiguration
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("buildRoot")]
        public string BuildRoot { get; set; }

        [JsonProperty("generator")]
        public string Generator { get; set; }
    }
}
