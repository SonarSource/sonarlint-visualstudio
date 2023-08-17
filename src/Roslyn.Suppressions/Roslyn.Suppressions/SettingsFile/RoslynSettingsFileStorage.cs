/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO.Abstractions;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ETW;
using SonarLint.VisualStudio.Roslyn.Suppressions.Resources;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile
{
    internal interface IRoslynSettingsFileStorage
    {
        /// <summary>
        /// Updates the Roslyn settings file on disc for the specified solution
        /// </summary>
        void Update(RoslynSettings settings, string solutionNameWithoutExtension);

        /// <summary>
        /// Return the settings for the specific settings key, or null
        /// if there are no settings for that key
        /// </summary>
        RoslynSettings Get(string settingsKey);
    }

    [Export(typeof(IRoslynSettingsFileStorage))]
    internal class RoslynSettingsFileStorage : IRoslynSettingsFileStorage
    {
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        [ImportingConstructor]
        public RoslynSettingsFileStorage(ILogger logger) : this(logger, new FileSystem())
        {
        }

        internal RoslynSettingsFileStorage(ILogger logger, IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
            this.logger = logger;
            fileSystem.Directory.CreateDirectory(RoslynSettingsFileInfo.Directory);
        }

        public RoslynSettings Get(string settingsKey)
        {
            Debug.Assert(settingsKey != null, "Not expecting settings to be null");
         
            try
            {
                CodeMarkers.Instance.FileStorageGetStart();
                var filePath = RoslynSettingsFileInfo.GetSettingsFilePath(settingsKey);

                if(!fileSystem.File.Exists(filePath))
                {
                    logger.WriteLine(string.Format(Strings.RoslynSettingsFileStorageGetError, settingsKey, Strings.RoslynSettingsFileStorageFileNotFound));
                    return null;
                }

                var fileContent = fileSystem.File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<RoslynSettings>(fileContent);
            }
            catch (Exception ex)
            {
                logger.WriteLine(string.Format(Strings.RoslynSettingsFileStorageGetError, settingsKey, ex.Message));
            }
            finally
            {
                CodeMarkers.Instance.FileStorageGetStop();
            }
            return null;
        }

        public void Update(RoslynSettings settings, string solutionNameWithoutExtension)
        {
            Debug.Assert(settings != null, "Not expecting settings to be null");
            Debug.Assert(!string.IsNullOrWhiteSpace(settings.SonarProjectKey), "Not expecting settings.SonarProjectKey to be null");
            Debug.Assert(solutionNameWithoutExtension != null, "Not expecting solutionNameWithoutExtension to be null");

            try
            {
                CodeMarkers.Instance.FileStorageUpdateStart();
                var filePath = RoslynSettingsFileInfo.GetSettingsFilePath(solutionNameWithoutExtension);
                var fileContent = JsonConvert.SerializeObject(settings, Formatting.Indented);
                fileSystem.File.WriteAllText(filePath, fileContent);
            }
            catch (Exception ex)
            {
                logger.WriteLine(string.Format(Strings.RoslynSettingsFileStorageUpdateError, settings.SonarProjectKey, ex.Message));
            }
            finally
            {
                CodeMarkers.Instance.FileStorageUpdateStop();
            }
        }
    }
}
