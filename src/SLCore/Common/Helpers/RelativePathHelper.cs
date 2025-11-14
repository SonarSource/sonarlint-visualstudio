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

namespace SonarLint.VisualStudio.SLCore.Common.Helpers;

internal static class RelativePathHelper
{
    private static readonly char Separator = Path.DirectorySeparatorChar;

    public static string GetRelativePathToRootFolder(string root, string filePath)
    {
        Validate(root, filePath);

        var commonParentPathLength = CalculateCommonPathLength(root, filePath);

        if (commonParentPathLength == 0)
        {
            return null;
        }

        var depthOfRootRelativeToCommonParent  = CalculateRemainingPathDepth(root, commonParentPathLength);
        var filePathRelativeToCommonParent = filePath.Substring(commonParentPathLength);

        return depthOfRootRelativeToCommonParent == 0
            ? filePathRelativeToCommonParent
            : MovePathUpwards(depthOfRootRelativeToCommonParent, filePathRelativeToCommonParent);
    }

    private static void Validate(string root, string filePath)
    {
        if (!IsPathFullyQualified(root))
        {
            throw new ArgumentException(string.Format(SLCoreStrings.RelativePathHelper_NonAbsolutePath, root), nameof(root));
        }

        if (!IsPathFullyQualified(filePath))
        {
            throw new ArgumentException(string.Format(SLCoreStrings.RelativePathHelper_NonAbsolutePath, filePath), nameof(filePath));
        }

        if (root[root.Length - 1] != Separator)
        {
            throw new ArgumentException(string.Format(SLCoreStrings.RelativePathHelper_RootDoesNotEndWithSeparator, Separator), nameof(root));
        }
    }

    /// <summary>
    /// Returns true if it is a fully qualified local or UNC path
    /// </summary>
    private static bool IsPathFullyQualified(string path)
    {
        var root = Path.GetPathRoot(path);
        return root.StartsWith(@"\\") || root.EndsWith(@"\") && root != @"\";
    }

    /// <summary>
    /// Constructs relative path by adding `..\` to filePathRelativeToCommonParent as many times, as it takes to get from root to common parent by doing `cd ..`
    /// </summary>
    private static string MovePathUpwards(int directoriesUp, string filePathRelativeToCommonParent)
    {
        var sb = new StringBuilder();

        for (var directoryNumber = 0; directoryNumber < directoriesUp; directoryNumber++)
        {
            sb.Append("..");
            sb.Append(Separator);
        }

        sb.Append(filePathRelativeToCommonParent);

        return sb.ToString();
    }

    /// <summary>
    /// Calculates the depth of root path relative to common parent
    /// </summary>
    private static int CalculateRemainingPathDepth(string root, int commonPathLength)
    {
        var depth = 0;
        for (var index = commonPathLength; index < root.Length; index++)
        {
            if (root[index] == Separator)
            {
                depth++;
            }
        }

        return depth;
    }

    /// <summary>
    /// Calculates the length of the common string prefix that ends on the directory separator, which is equivalent to the common parent path.
    /// If there is no common parent path, returns 0.
    /// </summary>
    private static int CalculateCommonPathLength(string path1, string path2)
    {
        int commonFilePathLenght;
        for (commonFilePathLenght = CalculateCommonPrefixLength(path1, path2); commonFilePathLenght > 0; commonFilePathLenght--)
        {
            var index = commonFilePathLenght - 1;
            if (path1[index] == Separator)
            {
                break;
            }
        }

        return commonFilePathLenght;
    }

    /// <summary>
    /// Calculates the length of the common string prefix. If there is no common prefix, returns 0.
    /// </summary>
    private static int CalculateCommonPrefixLength(string path1, string path2)
    {
        int index;
        for (index = 0; index < path1.Length && index < path2.Length; index++)
        {
            if (path1[index] != path2[index])
            {
                break;
            }
        }
        return index;
    }
}
