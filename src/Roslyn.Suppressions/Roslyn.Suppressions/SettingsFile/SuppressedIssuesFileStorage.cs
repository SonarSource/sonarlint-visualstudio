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
using System.IO.Abstractions;
using System.Linq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.Roslyn.Suppressions.Resources;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Roslyn.Suppression.SettingsFile
{
    internal class SuppressedIssuesFileStorage : ISuppressedIssuesFileStorage
    {
        private readonly string fileDirectory;
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        public SuppressedIssuesFileStorage(ILogger logger) : this(logger, new FileSystem())
        {
            
        }

        internal SuppressedIssuesFileStorage(ILogger logger, IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
            this.logger = logger;

            fileDirectory = Path.Combine(Path.GetTempPath(), "SLVS", "Roslyn");
            fileSystem.Directory.CreateDirectory(fileDirectory);
        }

        public IEnumerable<SonarQubeIssue> Get(string sonarProjectKey)
        {

            ValidateSonarProjectKey(sonarProjectKey);
            try
            {
                var escapedName = PathHelper.EscapeFileName(sonarProjectKey);
                var filePath = GetFilePath(escapedName);
                if(!fileSystem.File.Exists(filePath))
                {
                    logger.WriteLine(string.Format(Strings.SuppressedIssuesFileStorageGetError, sonarProjectKey, Strings.SuppressedIssuesFileStorageFileNotFound));
                    return Enumerable.Empty<SonarQubeIssue>();
                }

                var fileContent = fileSystem.File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<IEnumerable<SonarQubeIssue>>(fileContent);
            }
            catch (Exception ex)
            {
                logger.WriteLine(string.Format(Strings.SuppressedIssuesFileStorageGetError, sonarProjectKey, ex.Message));
            }

            return Enumerable.Empty<SonarQubeIssue>();
        }

        public void Update(string sonarProjectKey, IEnumerable<SonarQubeIssue> allSuppressedIssues)
        {
            ValidateSonarProjectKey(sonarProjectKey);
            try
            {
                var escapedName = PathHelper.EscapeFileName(sonarProjectKey);
                var filePath = GetFilePath(escapedName);
                var fileContent = JsonConvert.SerializeObject(allSuppressedIssues);
                fileSystem.File.WriteAllText(filePath, fileContent);
            }
            catch (Exception ex)
            {
                
                logger.WriteLine(string.Format(Strings.SuppressedIssuesFileStorageUpdateError, sonarProjectKey, ex.Message));
            }
        }

        private void ValidateSonarProjectKey(string sonarProjectKey)
        {
            if (string.IsNullOrWhiteSpace(sonarProjectKey))
            {
                throw new ArgumentException(Strings.SuppressedIssuesFileStorageEmptyProjectKey, nameof(sonarProjectKey));
            }
        }

        private string GetFilePath(string sonarProjectKey)
        {
            string fileName = sonarProjectKey + ".json";
            return Path.Combine(fileDirectory, fileName);
        }
    }
}
