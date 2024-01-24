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

using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;

// NOTE: don't directly export this class.
// We're exporting individual string using fixed contract names.
// See the ReadMe.txt for more information.

namespace SonarLint.VisualStudio.AdditionalFiles
{
    /// <summary>
    /// Exports paths for additional files relating to JavaScript/TypeScript analysis
    /// </summary>
    internal class TypeScriptPathsProvider
    {
        [Export("SonarLint.TypeScript.EsLintBridgeServerPath")]
        public string EsLintBridgeServerPath => GetInstallationPath("ts", "bin", "server");

        [Export("SonarLint.TypeScript.RuleDefinitionsFilePath")]
        public string RuleMetadataFilePath => GetInstallationPath("ts", "sonarlint-metadata.json");

        private static string GetInstallationPath(params string[] paths)
        {
            var subPath = Path.Combine(paths);
            var fullPath = Path.Combine(ExtensionFolderPath, subPath);
            Debug.Assert(File.Exists(fullPath), @"Could not find embedded file. Expected location: {fullPath}");
            return fullPath;
        }

        private static string ExtensionFolderPath => Path.GetDirectoryName(typeof(TypeScriptPathsProvider).Assembly.Location);
    }
}
