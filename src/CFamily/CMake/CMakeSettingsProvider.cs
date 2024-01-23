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
using System.IO.Abstractions;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.CFamily.CMake
{
    internal interface ICMakeSettingsProvider
    {
        /// <summary>
        /// Attempts to find and parse a CMakeSettings.json under the given root directory.
        /// </summary>
        /// <returns>Returns null if the file was not found or could not be parsed.</returns>
        CMakeSettingsSearchResult Find(string rootDirectory);
    }

    internal class CMakeSettingsSearchResult
    {
        public CMakeSettingsSearchResult(CMakeSettings settings, 
            string cMakeSettingsFilePath, 
            string rootCMakeListsFilePath)
        {
            Settings = settings;
            CMakeSettingsFilePath = cMakeSettingsFilePath;
            RootCMakeListsFilePath = rootCMakeListsFilePath;
        }

        public CMakeSettings Settings { get; }
        public string CMakeSettingsFilePath { get; }
        public string RootCMakeListsFilePath { get; }
    }

    internal class CMakeSettingsProvider : ICMakeSettingsProvider
    {
        internal const string CMakeSettingsFileName = "CMakeSettings.json";
        internal const string CMakeListsFileName = "CMakeLists.txt";

        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        public CMakeSettingsProvider(ILogger logger)
            : this(logger, new FileSystem())
        {
        }

        internal CMakeSettingsProvider(ILogger logger, IFileSystem fileSystem)
        {
            this.logger = logger;
            this.fileSystem = fileSystem;
        }

        public CMakeSettingsSearchResult Find(string rootDirectory)
        {
            var settingsFiles = FindSettingsFiles(rootDirectory);

            if (settingsFiles == null)
            {
                logger.LogVerbose($"[CMakeSettingsProvider] No matching CMakeSettings.json was found under {rootDirectory}");
                return null;
            }

            var cmakeSettingsFullPath = settingsFiles.Item1;
            var cmakeListsFullPath = settingsFiles.Item2;

            logger.LogVerbose($"[CMakeSettingsProvider] Reading {cmakeSettingsFullPath}...");

            try
            {
                var settingsString = fileSystem.File.ReadAllText(cmakeSettingsFullPath);
                var parsedSettings = JsonConvert.DeserializeObject<CMakeSettings>(settingsString);

                return new CMakeSettingsSearchResult(parsedSettings, cmakeSettingsFullPath, cmakeListsFullPath);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.BadCMakeSettings, ex.Message);
                return null;
            }
        }

        private Tuple<string, string> FindSettingsFiles(string rootDirectory)
        {
            try
            {
                var cmakeSettingsFiles = fileSystem
                    .Directory
                    .EnumerateFiles(rootDirectory, CMakeSettingsFileName, SearchOption.AllDirectories);

                foreach (var cmakeSettingsFile in cmakeSettingsFiles)
                {
                    logger.LogVerbose($"[CMakeSettingsProvider] Found candidate CMakeSettings.json: {cmakeSettingsFile}...");

                    var directory = Path.GetDirectoryName(cmakeSettingsFile);
                    var cmakeListsFile = Path.Combine(directory, CMakeListsFileName);
                    var hasCMakeListsNextToIt = fileSystem.File.Exists(cmakeListsFile);

                    if (hasCMakeListsNextToIt)
                    {
                        return new Tuple<string, string>(cmakeSettingsFile, cmakeListsFile);
                    }

                    logger.LogVerbose($"[CMakeSettingsProvider] No CMakeLists.txt was found next to {cmakeSettingsFile}");
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.LogVerbose($"[CMakeSettingsProvider] Failed to locate CMakeSettings.json: {ex}");
                logger.WriteLine(Resources.FailedToFindCMakeSettings, ex.Message);
            }

            return null;
        }
    }
}
