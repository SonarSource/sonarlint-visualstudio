/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System.Diagnostics;

namespace SonarQube.Client.Helpers
{
    public static class FilePathNormalizer
    {
        private const char WindowsFilePathSeparator = '\\';
        private const char SonarServerFilePathSeparator = '/';
        
        /// <summary>
        /// Converts SQ file path format into Windows file path format.
        /// </summary>
        /// <remarks>
        /// Forward-slashes are replaced with back-slashes.
        /// Opening slashes are removed.
        /// </remarks>
        public static string NormalizeSonarQubePath(string path)
        {
            Debug.Assert(path == null || !path.Contains(WindowsFilePathSeparator.ToString()),
                $"Expecting sonarqube relative path delimiters to be forward-slash but got '{path}'.");

            return path?.Trim(SonarServerFilePathSeparator).Replace(SonarServerFilePathSeparator, WindowsFilePathSeparator)
                   ?? string.Empty;
        }

        /// <summary>
        /// Converts Windows file path format into SQ file path format.
        /// </summary>
        /// <remarks>
        /// Back-slashes are replaced with forward-slashes.
        /// Opening slashes are removed.
        /// </remarks>
        public static string ServerizeWindowsPath(string path)
        {
            Debug.Assert(path == null || !path.Contains(SonarServerFilePathSeparator.ToString()),
                $"Expecting windows relative path delimiters to be back-slash but got '{path}'.");
            
            return path?.Trim(WindowsFilePathSeparator).Replace(WindowsFilePathSeparator, SonarServerFilePathSeparator) ?? string.Empty;
        }
    }
}
