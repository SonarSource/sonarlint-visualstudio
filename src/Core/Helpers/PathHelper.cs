﻿/*
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SonarLint.VisualStudio.Core.Helpers
{
    public static class PathHelper
    {
        private static readonly Guid perVSInstanceFolderName = Guid.NewGuid();

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
        /// Append a <see cref="Path.DirectorySeparatorChar"/> to the end of the string if one does not already exist.
        /// </summary>
        public static string ForceDirectoryEnding(string str)
        {
            if (!str.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                return str + Path.DirectorySeparatorChar;
            }

            return str;
        }

        /// <summary>
        /// Compute the path of <paramref name="toFullPath"/>, relative to <paramref name="fromFullPath"/>.
        /// </summary>
        /// <param name="fromFullPath">Path with which to make <paramref name="toFullPath"/> relative to</param>
        /// <param name="toFullPath">Absolute path</param>
        /// <returns>Relative path if one exists, otherwise the original <paramref name="toFullPath"/></returns>
        public static string CalculateRelativePath(string fromFullPath, string toFullPath)
        {
            if (string.IsNullOrWhiteSpace(toFullPath))
            {
                throw new ArgumentNullException(nameof(toFullPath));
            }

            if (string.IsNullOrWhiteSpace(fromFullPath))
            {
                throw new ArgumentNullException(nameof(fromFullPath));
            }

            Uri absoluteUri;
            Uri relativeToUri;

            if (!Uri.TryCreate(toFullPath, UriKind.Absolute, out absoluteUri))
            {
                throw new ArgumentException(CoreStrings.PathHelperAbsolutePathExpected, nameof(toFullPath));
            }

            if (!Uri.TryCreate(fromFullPath, UriKind.Absolute, out relativeToUri))
            {
                throw new ArgumentException(CoreStrings.PathHelperAbsolutePathExpected, nameof(fromFullPath));
            }

            Uri relativeUri = relativeToUri.MakeRelativeUri(absoluteUri);
            return ToFilePathString(relativeUri);
        }

        /// <summary>
        /// Resolve a relative path against the given root path.
        /// </summary>
        /// <param name="relativePath">Relative path to resolve</param>
        /// <param name="resolutionRootFullPath">Root full path for the resolution of <paramref name="relativePath"/></param>
        /// <returns>Full absolute path</returns>
        public static string ResolveRelativePath(string relativePath, string resolutionRootFullPath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            if (string.IsNullOrWhiteSpace(resolutionRootFullPath))
            {
                throw new ArgumentNullException(nameof(resolutionRootFullPath));
            }

            Uri rootUri;

            if (!Uri.TryCreate(resolutionRootFullPath, UriKind.Absolute, out rootUri))
            {
                throw new ArgumentException(CoreStrings.PathHelperAbsolutePathExpected, nameof(resolutionRootFullPath));
            }

            Uri expandedUri = new Uri(rootUri, relativePath);
            return ToFilePathString(expandedUri);
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
        /// Returns true if a local file path and a server file path should be considered to be equivalent, otherwise false
        /// </summary>
        /// <param name="absoluteLocalFilePath">The normalized local absolute file path. Can be null/empty.</param>
        /// <param name="relativeServerFilePath">The normalized relative path from the server.  Can be null/empty.</param>
        /// <remarks>
        /// <paramref name="relativeServerFilePath"/> is expected to have been normalized by <see cref="SonarQube.Client.Helpers.FilePathNormalizer"/>.
        /// Either parameter can be null/empty, in which case they are treated as a module (project) level issue. In that case
        /// there will only be match if the other parameter is also a module level issue.
        /// <para>
        /// Implementation: the server path is tail-matched against the local path
        /// e.g. the server path "aaa\foo.txt" will match ALL of the following absolute paths:
        /// * C:\aaa\foo.txt
        /// * C:\bbb\aaa\foo.txt
        /// * D:\bbb\foo.txt
        /// It will not match e.g. C:\XXXaaa\foo.txt
        /// </para>
        /// </remarks>
        public static bool IsServerFileMatch(string absoluteLocalFilePath, string relativeServerFilePath)
        {
            var localPath = absoluteLocalFilePath ?? string.Empty;
            var serverPath = relativeServerFilePath ?? string.Empty;

            // A null/empty path means it's a module (project) level issue, and can only match
            // another module-level issue.
            if (localPath == string.Empty || serverPath == string.Empty)
            {
                return localPath == serverPath;
            }

            Debug.Assert(Path.IsPathRooted(localPath) && !localPath.Contains("/") && !localPath.Contains("..") && !localPath.Contains("\\.\\"),
                $"Expecting the client-side file path to be a normalized absolute path with only back-slashes delimiters. Actual: {absoluteLocalFilePath}");

            // NB all server file paths should have been normalized. See SonarQube.Client.Helpers.FilePathNormalizer.
            Debug.Assert(!Path.IsPathRooted(serverPath) && !serverPath.Contains("/"),
                $"Expecting the server-side file path to be relative and not to contain forward-slashes. Actual: {relativeServerFilePath}");

            Debug.Assert(!serverPath.StartsWith("\\"), "Not expecting server file path to start with a back-slash");
            if (localPath.EndsWith(serverPath, StringComparison.OrdinalIgnoreCase))
            {
                // Check the preceding local path character is a backslash  - we want to make sure a server path
                // of `aaa\foo.txt` matches `c:\aaa\foo.txt` but not `c:`bbbaaa\foo.txt`
                return localPath.Length > serverPath.Length &&
                    localPath[localPath.Length - serverPath.Length - 1] == '\\';
            }

            return false;
        }

        private static string ToFilePathString(Uri uri)
        {
            string escapedPath = uri.OriginalString.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return Uri.UnescapeDataString(escapedPath);
        }

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
                return Path.Combine(taskPath, perVSInstanceFolderName.ToString());
            }

            return taskPath;            
        }
    }
}
