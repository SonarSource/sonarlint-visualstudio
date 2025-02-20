/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

        var commonPathLength = CalculateCommonPathLength(root, filePath);

        if (commonPathLength == 0)
        {
            return null;
        }

        var directoriesUp  = CalculateRemainingPathDepth(root, commonPathLength);
        var relativeSubPath = filePath.Substring(commonPathLength);

        return directoriesUp == 0 ? relativeSubPath : MovePathUpwards(directoriesUp, relativeSubPath);
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

    private static bool IsPathFullyQualified(string path)
    {
        var root = Path.GetPathRoot(path);
        return root.StartsWith(@"\\") || root.EndsWith(@"\") && root != @"\";
    }

    private static string MovePathUpwards(int directoriesUp, string substring)
    {
        var sb = new StringBuilder();

        for (var directoryNumber = 0; directoryNumber < directoriesUp; directoryNumber++)
        {
            sb.Append("..");
            sb.Append(Separator);
        }

        sb.Append(substring);

        return sb.ToString();
    }

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
