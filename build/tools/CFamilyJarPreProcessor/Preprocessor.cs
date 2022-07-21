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

using System;
using System.Collections.Generic;
using System.IO;
using CFamilyJarPreProcessor.FileGenerator;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using static CFamilyJarPreProcessor.Common;

namespace CFamilyJarPreProcessor
{
    internal class Preprocessor
    {
        // The txz archive containing the subprocess.exe
        private const string WindowsTxzFilePattern = "clang*-win.txz";

        // Sub-folder into which the tar file should be unzipped
        private const string TarUnzipSubFolder = "tar_xz";

        // List of patterns to match single static files to copy from the jar to the output
        private readonly string[] StaticFile_SingleFilePatterns = new string[]
        {
                TarUnzipSubFolder + @"\subprocess.exe",
                TarUnzipSubFolder + @"\LICENSE_THIRD_PARTY.txt"
        };

        private readonly string[] InputRulesFiles_SingleFilePatterns = new string[]
        {
                "Sonar_way_profile.json",
                "RulesList.json",
        };

        private readonly string[] InputRulesFiles_MultipleFilesPatterns = new string[]
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
            var pluginFileName = ExtractPluginFileNameFromUrl(downloadUrl, logger);

            // Ensure working directories exists
            var localWorkingFolder = GetLocalBuildTimePluginCacheDir("SLVS_CFamily_Build");
            var perVersionPluginFolder = Path.Combine(localWorkingFolder, Path.GetFileNameWithoutExtension(pluginFileName));
            EnsureWorkingDirectoryExist(perVersionPluginFolder, logger);

            // Download and unzip the jar
            var jarFilePath = Path.Combine(perVersionPluginFolder, pluginFileName);
            DownloadJarFile(downloadUrl, jarFilePath, logger);
            UnzipJar(jarFilePath, perVersionPluginFolder, logger);

            // Uncompress and extract the windows tar archive to get the subprocess exe
            var tarFilePath = FindSingleFile(perVersionPluginFolder, WindowsTxzFilePattern, logger);
            var tarSubFolder = Path.Combine(perVersionPluginFolder, TarUnzipSubFolder);
            UncompressAndUnzipTgx(tarFilePath, tarSubFolder, logger);

            CopyRequiredStaticFilesToOutput(perVersionPluginFolder, destinationDir);

            GenerateRuleSettingsFiles(perVersionPluginFolder, destinationDir);
        }

        private void CopyRequiredStaticFilesToOutput(string searchRoot, string destinationDir)
        {
            var fileList = FindSingleFiles(searchRoot, StaticFile_SingleFilePatterns, logger);
            CopyFiles(fileList, destinationDir, logger);
        }
            
        private void GenerateRuleSettingsFiles(string searchRootDir, string destinationDir)
        {
            var inputRulesDirectory = CollectInputRulesDataInSingleFolder(searchRootDir);

            GenerateRuleSettingsFile(SonarLanguageKeys.C, inputRulesDirectory, destinationDir);
            GenerateRuleSettingsFile(SonarLanguageKeys.CPlusPlus, inputRulesDirectory, destinationDir);
        }

        private string CollectInputRulesDataInSingleFolder(string searchRootDir)
        {
            // We're re-using the old RulesLoader class to find and load all of
            // the individual rules json files. They are in multiple different
            // directories in the jar, but the RulesLoader expects them to be in
            // a single folder.
            // Rather than re-write the loader, we'll copy the files into a temp
            // folder for now.

            var fileList = FindInputRulesFiles(searchRootDir);

            var tempRulesFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            EnsureWorkingDirectoryExist(tempRulesFolder, logger);

            CopyFiles(fileList, tempRulesFolder, logger);

            return tempRulesFolder;
        }

        private List<string> FindInputRulesFiles(string searchRoot)
        {
            var files = new List<string>();

            files.AddRange(FindSingleFiles(searchRoot, InputRulesFiles_SingleFilePatterns, logger));
            files.AddRange(FindMultipleFiles(searchRoot, InputRulesFiles_MultipleFilesPatterns, logger));

            return files;
        }

        private void GenerateRuleSettingsFile(string language, string inputRulesDirectory, string destinationDir)
        {
            logger.LogMessage($"Generating rules setting file: {language}");
            var settings = RulesSettingsGenerator.Create(language, inputRulesDirectory, logger);

            var fullFilePath = Path.Combine(destinationDir, $"all_{language}.json");
            string dataAsText = JsonConvert.SerializeObject(settings, Formatting.Indented);

            logger.LogMessage($"Writing file: {fullFilePath}");
            File.WriteAllText(fullFilePath, dataAsText);
        }
    }
}
