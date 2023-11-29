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

using System;
using System.IO;

namespace SonarQube.Client.Helpers
{
    public static class ComponentKeyGenerator
    {
        /// <summary>
        /// Generates Sonar server component key
        /// </summary>
        /// <param name="localFilePath">Path for the local file.</param>
        /// <param name="projectRootPath">Path for the local root of the project. Server paths are relative to this.</param>
        /// <param name="projectKey">Sonar server project key</param>
        /// <returns>Component key in the format projectkey:relativepath</returns>
        /// <exception cref="ArgumentException">Invalid root format or the local path is not under the root.</exception>
        public static string GetComponentKey(string localFilePath, string projectRootPath, string projectKey)
        {
            if (!Path.IsPathRooted(projectRootPath) || !projectRootPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                throw new ArgumentException("Invalid root path format");
            }

            if (!localFilePath.StartsWith(projectRootPath))
            {
                throw new ArgumentException("Local path is not under this root");
            }
            
            var serverFilePath = FilePathNormalizer.ServerizeWindowsPath(localFilePath.Substring(projectRootPath.Length));
            return $"{projectKey}:{serverFilePath}";
        }
    }
}
