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

namespace SonarLint.VisualStudio.CFamily.CMake
{
    internal interface IEvaluationContext
    {
        /// <summary>
        /// The full path of the workspace folder (the currently opened folder)
        /// </summary>
        string RootDirectory { get; }

        /// <summary>
        /// The name of the active build configuration
        /// </summary>
        string ActiveConfiguration { get; }

        /// <summary>
        /// The name of the CMake generator used in this configuration
        /// </summary>
        string Generator { get; }

        /// <summary>
        /// The full path to the CMakeSettings.json
        /// </summary>
        string CMakeSettingsFilePath { get; }

        /// <summary>
        /// The full path to the root CMakeLists.txt
        /// </summary>
        string RootCMakeListsFilePath { get; }
    }

    internal class EvaluationContext : IEvaluationContext
    {
        public EvaluationContext(string activeConfiguration,
            string rootDirectory,
            string generator, 
            string rootCMakeListsFilePath, 
            string cMakeSettingsFilePath)
        {
            ActiveConfiguration = activeConfiguration;
            RootDirectory = rootDirectory;
            Generator = generator;
            RootCMakeListsFilePath = rootCMakeListsFilePath;
            CMakeSettingsFilePath = cMakeSettingsFilePath;
        }

        public string RootDirectory { get; }
        public string ActiveConfiguration { get; }
        public string Generator { get; }
        public string CMakeSettingsFilePath { get; }
        public string RootCMakeListsFilePath { get; }
    }
}
