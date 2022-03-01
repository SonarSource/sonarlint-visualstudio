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
