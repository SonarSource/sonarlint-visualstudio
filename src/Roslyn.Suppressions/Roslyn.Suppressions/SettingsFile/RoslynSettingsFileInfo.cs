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

using System.IO;
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile
{
    internal static class RoslynSettingsFileInfo
    {
        public static readonly string Directory = PathHelper.GetTempDirForTask(false, "Roslyn");

        /// <summary>
        /// Returns the full file path for the given solution name
        /// </summary>
        /// <param name="solutionName">The solution name without the file extension e.g. "MySolution"</param>
        /// <remarks> File will be in a shared location and would be able to be accessed by multiple instances of VS simulateneously</remarks>
        public static string GetSettingsFilePath(string solutionName)
        {
            var escapedName = PathHelper.EscapeFileName(NormalizeKey(solutionName));

            var fileName = escapedName + ".json";

            return Path.Combine(Directory, fileName);
        }

        /// <summary>
        /// Returns an identifier for the project settings to which the settings file relates.
        /// </summary>
        /// <remarks> The identifier is *not* the actual project key since we can't recover it accurately from the file name </remarks>
        public static string GetSettingsKey(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

        private static string NormalizeKey(string key) => key.ToLowerInvariant();
    }
}
