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

using System;
using System.IO;
using System.Linq;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.CFamily
{
    public static class CFamilyShared
    {
        public static readonly string CFamilyFilesDirectory = Path.Combine(
            Path.GetDirectoryName(typeof(CFamilyShared).Assembly.Location),
            "lib");

        public static readonly StringComparer RuleKeyComparer = SonarRuleRepoKeys.RepoKeyComparer;
        public static readonly StringComparison RuleKeyComparison = StringComparison.Ordinal;

        public static readonly string[] KnownExtensions = {".cpp", ".cxx", ".cc", ".c"};

        private static readonly string[] KnownHeaderFileExtensions = {".h", ".hpp", ".hh", ".hxx"};

        /// <summary>
        /// Attempts to detect whether the file is C or C++ based on the file extension.
        /// Returns null if the extension is not recognised.
        /// </summary>
        public static string FindLanguageFromExtension(string analyzedFilePath)
        {
            string cfamilyLanguage = null;

            // Compile files with extensions ".cpp", ".cxx" and ".cc" as Cpp and files with extension ".c" as C
            if (analyzedFilePath.EndsWith(".cpp", StringComparison.OrdinalIgnoreCase) ||
                analyzedFilePath.EndsWith(".cxx", StringComparison.OrdinalIgnoreCase) ||
                analyzedFilePath.EndsWith(".cc", StringComparison.OrdinalIgnoreCase))
            {
                cfamilyLanguage = SonarLanguageKeys.CPlusPlus;
            }
            else if (analyzedFilePath.EndsWith(".c", StringComparison.OrdinalIgnoreCase))
            {
                cfamilyLanguage = SonarLanguageKeys.C;
            }

            return cfamilyLanguage;
        }

        /// <summary>
        /// Returns true/false if the file's extension is a known header file extension.
        /// </summary>
        public static bool IsHeaderFileExtension(string filePath)
        {
            var extension = Path.GetExtension(filePath);

            return KnownHeaderFileExtensions.Any(x =>
                x.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }
    }
}
