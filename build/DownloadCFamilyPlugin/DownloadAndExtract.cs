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
    /// Downloads the CFamily jar and extracts the files that need to be
    /// embedded in the SLVS vsix
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// * we are downloading the CFamily plugin jar e.g. https://binaries.sonarsource.com/CommercialDistribution/sonar-cfamily-plugin/sonar-cfamily-plugin-6.3.0.11371.jar
    /// * the jar contains a .tarxz archive containing the Windows version of SubProcess.exe
    /// * the jar contains various .json files, some with fixed names, some that we'll match using wildcards
    /// 
    /// We want the task to fail if it cannot locate all of the expected files (otherwise we'll build an invalid VSIX).
    /// 
    /// Downloading and extracting the files can be slow so we want to skip those steps if possible.
    /// </remarks>
    public class DownloadAndExtract: Task
    {
        // The txz archive containing the subprocess.exe
        private const string WindowsTxzFilePattern = "clang*-win.txz";

        // Sub-folder into which the tar file should be unzipped
        private const string TarUnzipSubFolder = "tar_xz";

        // List of patterns to match single files in the uncompressed output
        private readonly string[] SingleFilePatterns = new string[]
        {
                "Sonar_way_profile.json",
                "RulesList.json",
                TarUnzipSubFolder + @"\subprocess.exe",
                TarUnzipSubFolder + @"\LICENSE_THIRD_PARTY.txt"
        };

        // List of patterns to match multiple files in the uncompressed output
        private readonly string[] MultipleFilesPatterns = new string[]
        {
            @"org\sonar\l10n\cpp\rules\params\*_params.json",
            @"org\sonar\l10n\cpp\rules\cpp\*.json"
        };

        #region MSBuild input / output properties (set/read in the project file)

        // Example: https://binaries.sonarsource.com/CommercialDistribution/sonar-cfamily-plugin/sonar-cfamily-plugin-6.3.0.11371.jar
        [Required]
        public string DownloadUrl { get; set; }

        [Output]
        public string[] FilesToEmbed { get; private set; }

        #endregion

        public override bool Execute()
        {
            LogMessage($"Download url: {DownloadUrl}");

            var pluginFileName = Common.ExtractPluginFileNameFromUrl(DownloadUrl, Log);

            // Ensure working directories exists
            var localWorkingFolder = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), "SLVS_CFamily_Build");
            var perVersionPluginFolder = Path.Combine(localWorkingFolder, Path.GetFileNameWithoutExtension(pluginFileName));
            Common.EnsureWorkingDirectoryExist(perVersionPluginFolder, this.Log);

            // Download and unzip the jar
            var jarFilePath = Path.Combine(perVersionPluginFolder, pluginFileName);
            Common.DownloadJarFile(DownloadUrl, jarFilePath, Log);
            Common.UnzipJar(jarFilePath, perVersionPluginFolder, Log);

            // Uncompress and extract the windows tar archive to get the subprocess exe
            var tarFilePath = Common.FindSingleFile(perVersionPluginFolder, WindowsTxzFilePattern, Log);
            var tarSubFolder = Path.Combine(perVersionPluginFolder, TarUnzipSubFolder);
            Common.UncompressAndUnzipTgx(tarFilePath, tarSubFolder, Log);

            // Locate the required files from the uncompressed jar and tar
            var fileList = FindFiles(perVersionPluginFolder);

            FilesToEmbed = fileList.ToArray();
            return !Log.HasLoggedErrors;
        }

        private List<string> FindFiles(string searchRoot)
        {
            var files = new List<string>();

            files.AddRange(Common.FindSingleFiles(searchRoot, SingleFilePatterns, Log));
            files.AddRange(Common.FindMultipleFiles(searchRoot, MultipleFilesPatterns, Log));

            return files;
        }

        private void LogMessage(string message)
        {
            Log.LogMessage(MessageImportance.High, message);
        }
    }
}
