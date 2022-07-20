/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

using System.Collections.Generic;
using System.IO;

namespace CFamilyJarPreProcessor
{
    internal class Preprocessor
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

        private readonly ILogger logger;
        public Preprocessor(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Downloads plugin, extracts files, and copies the requires file to the output folder 
        /// </summary>
        /// <remarks>The method is lazy i.e. won't download the plugin if it exists, won't copy a file
        /// unless it is newer than the target file.</remarks>
        public void Execute(string downloadUrl, string destinationDir)
        {
            var pluginFileName = Common.ExtractPluginFileNameFromUrl(downloadUrl, logger);

            // Ensure working directories exists
            var localWorkingFolder = Common.GetLocalBuildTimePluginCacheDir("SLVS_CFamily_Build");
            var perVersionPluginFolder = Path.Combine(localWorkingFolder, Path.GetFileNameWithoutExtension(pluginFileName));
            Common.EnsureWorkingDirectoryExist(perVersionPluginFolder, logger);

            // Download and unzip the jar
            var jarFilePath = Path.Combine(perVersionPluginFolder, pluginFileName);
            Common.DownloadJarFile(downloadUrl, jarFilePath, logger);
            Common.UnzipJar(jarFilePath, perVersionPluginFolder, logger);

            // Uncompress and extract the windows tar archive to get the subprocess exe
            var tarFilePath = Common.FindSingleFile(perVersionPluginFolder, WindowsTxzFilePattern, logger);
            var tarSubFolder = Path.Combine(perVersionPluginFolder, TarUnzipSubFolder);
            Common.UncompressAndUnzipTgx(tarFilePath, tarSubFolder, logger);

            // Locate the required files from the uncompressed jar and tar
            var fileList = FindFiles(perVersionPluginFolder);

            // Copy the files to the output directory
            CopyFilesToOutputDirectory(fileList, destinationDir);
        }

        private List<string> FindFiles(string searchRoot)
        {
            var files = new List<string>();

            files.AddRange(Common.FindSingleFiles(searchRoot, SingleFilePatterns, logger));
            files.AddRange(Common.FindMultipleFiles(searchRoot, MultipleFilesPatterns, logger));

            return files;
        }

        private void CopyFilesToOutputDirectory(IList<string> files, string destinationDir)
        {
            Common.EnsureWorkingDirectoryExist(destinationDir, logger);

            foreach(var file in files)
            {
                CopyIfNewer(file, destinationDir);
            }
        }

        private void CopyIfNewer(string file, string destinationDir)
        {
            // Overwrite if newer
            var sourceFileInfo = new FileInfo(file);
            var destinationFileInfo = new FileInfo(Path.Combine(destinationDir, sourceFileInfo.Name));

            if (!destinationFileInfo.Exists || sourceFileInfo.LastWriteTimeUtc > destinationFileInfo.LastWriteTimeUtc)
            {
                logger.LogMessage($"  Copying file: {file}");
                File.Copy(sourceFileInfo.FullName, destinationFileInfo.FullName, true);
            }
            else
            {
                logger.LogMessage($"  Skipping copying file - not newer: {file}");
            }
        }
    }
}
