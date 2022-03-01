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
using SonarLint.VisualStudio.Core.Suppressions;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Suppression
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
        }

        public IEnumerable<SonarQubeIssue> Get(string sonarProjectKey)
        {
            try
            {
                if (!ValidateFileName(sonarProjectKey))
                {
                    return Enumerable.Empty<SonarQubeIssue>();
                }

                var filePath = GetFilePath(sonarProjectKey);
                var fileContent = fileSystem.File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<IEnumerable<SonarQubeIssue>>(fileContent);
            }
            catch (Exception ex)
            {
                logger.WriteLine($"SuppressedIssuesFileStorage Get Error: {ex.Message}");
            }

            return Enumerable.Empty<SonarQubeIssue>();
        }

        public void Update(string sonarProjectKey, IEnumerable<SonarQubeIssue> allSuppressedIssues)
        {
            try
            {
                if (!ValidateFileName(sonarProjectKey))
                {
                    return;
                }

                var filePath = GetFilePath(sonarProjectKey);
                var fileContent = JsonConvert.SerializeObject(allSuppressedIssues);
                fileSystem.Directory.CreateDirectory(fileDirectory);
                fileSystem.File.WriteAllText(filePath, fileContent);
            }
            catch (Exception ex)
            {
                logger.WriteLine($"SuppressedIssuesFileStorage Update Error: {ex.Message}");
            }
        }

        private bool ValidateFileName(string sonarProjectKey)
        {
            if (string.IsNullOrWhiteSpace(sonarProjectKey))
            {
                logger.WriteLine("SuppressedIssuesFileStorage: File name should not be empty");
                return false;
            }
            if (sonarProjectKey.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                logger.WriteLine("SuppressedIssuesFileStorage: File name has illegal characters");
                return false;
            }
            return true;
        }

        private string GetFilePath(string sonarProjectKey)
        {
            string fileName = sonarProjectKey + ".json";
            return Path.Combine(fileDirectory, fileName);
        }
    }
}
