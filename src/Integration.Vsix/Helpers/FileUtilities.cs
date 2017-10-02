/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Runtime.InteropServices;
using System.Text;

namespace SonarLint.VisualStudio.Integration.Vsix.Helpers
{
    internal static class FileUtilities
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        private static extern bool PathRelativePathTo(
            [Out] StringBuilder pszPath,
            [In] string pszFrom,
            [In] FileAttributes dwAttrFrom,
            [In] string pszTo,
            [In] FileAttributes dwAttrTo);

        /// <summary>
        /// Returns the relative path to the second file from the first, or 
        /// <paramref name="toFilePath"/> if the paths are not relative.
        /// </summary>
        public static string GetRelativePath(string fromFilePath, string toFilePath)
        {
            var sb = new StringBuilder(260);
            bool success = PathRelativePathTo(sb, fromFilePath, FileAttributes.Normal, toFilePath, FileAttributes.Normal);

            if (success)
            {
                // Strip of the the leading ".\" (i.e. if the files are in the same directory)
                int offset = 0;
                if (sb[0] == '.' && sb[1] == '\\')
                {
                    offset = 2;
                }
                return sb.ToString(offset, sb.Length - offset);
            }

            return toFilePath;
        }

    }
}
