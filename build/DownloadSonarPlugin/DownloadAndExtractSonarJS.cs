/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DownloadCFamilyPlugin
{
    /// <summary>
    /// Downloads the Sonar JavaScript/TypeScript jar and extracts the files that need to be
    /// embedded in the SLVS vsix
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// * we are downloading the sonar-js plugin jar e.g. https://binaries.sonarsource.com/Distribution/sonar-javascript-plugin/sonar-javascript-plugin-6.2.0.12043.jar
    /// * the jar contains a .tgz archive containing ESLintBridge (a NodeJS app)
    /// * the jar contains various .json files, some with well-known names.
    /// 
    /// We want the task to fail if it cannot locate all of the expected files (otherwise we'll build an invalid VSIX).
    /// 
    /// Downloading and extracting the files can be slow so we want to skip those steps if possible.
    /// </remarks>
    public class DownloadAndExtractSonarJS: Task
    {
        // Structure of the archive file:
        // 
        //      eslint-bridge-1.0.0.tgz -> eslint-bridge-1.0.0.tar ->
        //          package\bin\
        //          package\lib\
        //          package\node_modules\
        //          package\package.json

        // The archive containing the eslintbridge
        private const string SourceEsLintBridgeFilePattern = "sonarjs-*.tgz";

        // Sub-folder into which the tar file should be unzipped
        public const string TargetEslintBridgeFolderName = "sonarjs";

        // List of patterns to match single files in the uncompressed output
        private readonly string[] SourceSingleFilePatterns = new string[]
        {
                "sonarlint-metadata.json"
        };

        private readonly string SourceRelativePackageDirectory = TargetEslintBridgeFolderName + @"\package";

        #region MSBuild input / output properties (set/read in the project file)

        // Download url example: https://binaries.sonarsource.com/Distribution/sonar-javascript-plugin/sonar-javascript-plugin-6.2.0.12043.jar
        [Required]
        public string DownloadUrl { get; set; }

        [Output]
        public string[] FilesToEmbed { get; private set; }

        [Output]
        public string PackageDirectoryToEmbed { get; private set; }

        #endregion

        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.High, $"Download url: {DownloadUrl}");

            var pluginFileName = Common.ExtractPluginFileNameFromUrl(DownloadUrl, Log);

            // Ensure working directories exists
            var localWorkingFolder = Common.GetLocalBuildTimePluginCacheDir("SLVS_TypeScript_Build");
            var perVersionPluginFolder = Path.Combine(localWorkingFolder, Path.GetFileNameWithoutExtension(pluginFileName));
            Common.EnsureWorkingDirectoryExist(perVersionPluginFolder, this.Log);

            // Download and unzip the jar
            var jarFilePath = Path.Combine(perVersionPluginFolder, pluginFileName);
            Common.DownloadJarFile(DownloadUrl, jarFilePath, Log);
            Common.UnzipJar(jarFilePath, perVersionPluginFolder, Log);

            // Uncompress and extract the windows tar archive to get the eslint-bridge folder
            var tarFilePath = Common.FindSingleFile(perVersionPluginFolder, SourceEsLintBridgeFilePattern, Log);
            var tarSubFolder = Path.Combine(perVersionPluginFolder, TargetEslintBridgeFolderName);
            Common.UncompressAndUnzipTgz(tarFilePath, tarSubFolder, Log);

            // Locate the required files from the uncompressed jar and tar
            var fileList = FindFiles(perVersionPluginFolder);
            FilesToEmbed = fileList.ToArray();

            PackageDirectoryToEmbed = Path.Combine(perVersionPluginFolder, SourceRelativePackageDirectory);

            return !Log.HasLoggedErrors;
        }

        private List<string> FindFiles(string searchRoot)
        {
            var files = new List<string>();
            files.AddRange(Common.FindSingleFiles(searchRoot, SourceSingleFilePatterns, Log));
            return files;
        }
    }
}
