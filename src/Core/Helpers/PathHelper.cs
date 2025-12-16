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

using System.IO;
using System.Text;

namespace SonarLint.VisualStudio.Core.Helpers
{
    public static class PathHelper
    {
        internal static readonly Guid PerVsInstanceFolderName = Guid.NewGuid();

        /// <summary>
        /// Replace all invalid file path characters with the underscore ("_").
        /// </summary>
        public static string EscapeFileName(string unescapedName)
        {
            const char safeChar = '_';

            if (string.IsNullOrWhiteSpace(unescapedName))
            {
                return unescapedName;
            }

            IEnumerable<char> invalidChars = Enumerable.Union(Path.GetInvalidPathChars(), Path.GetInvalidFileNameChars());
            var escapedName = new StringBuilder(unescapedName);
            foreach (var invalidChar in invalidChars)
            {
                escapedName.Replace(invalidChar, safeChar);
            }

            return escapedName.ToString();
        }

        /// <summary>
        /// Gets whether or not the given <paramref name="path"/> has exists under the given <paramref name="rootDirectory"/>.
        /// </summary>
        public static bool IsPathRootedUnderRoot(string path, string rootDirectory)
        {
            Debug.Assert(Path.IsPathRooted(path) || Path.IsPathRooted(rootDirectory), "Path should be absolute");

            string expandedPath = Path.GetFullPath(path);
            string expandedRootDirectory = Path.GetFullPath(rootDirectory);

            return expandedPath.StartsWith(expandedRootDirectory, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsMatchingPath(string path1, string path2) =>
            Path.GetFullPath(path1).Equals(Path.GetFullPath(path2), StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets Temp Directory under SLVS folder under %Temp%
        /// </summary>
        /// <param name="perVSInstance">Determines if the path will be per VS Instance</param>
        /// <param name="folders">Creates sub folder structure under SLVS folder</param>
        public static string GetTempDirForTask(bool perVSInstance, params string[] folders)
        {
            var SLVSTempFolder = Path.Combine(Path.GetTempPath(), "SLVS");

            var taskFolders = new List<string> { SLVSTempFolder };

            taskFolders.AddRange(folders);

            var taskPath = Path.Combine(taskFolders.ToArray());

            if(perVSInstance)
            {
                return Path.Combine(taskPath, PerVsInstanceFolderName.ToString());
            }

            return taskPath;
        }
    }
}
