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

namespace SonarLint.VisualStudio.Core.VsInfo
{
    public interface IVsVersion
    {
        /// <summary>
        /// VS full product name, including edition. Example: "Visual Studio Enterprise 2022"
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// VS build version. Example: "16.9.30914.41"
        /// </summary>
        string InstallationVersion { get; }

        /// <summary>
        /// VS display version, as seen in the About window. Example: "16.9.0 Preview 3.0"
        /// </summary>
        string DisplayVersion { get; }

        /// <summary>
        /// Major VS build version. Example: for "16.9.30914.41" it'll be "16"
        /// </summary>
        string MajorInstallationVersion { get; }
    }
}
