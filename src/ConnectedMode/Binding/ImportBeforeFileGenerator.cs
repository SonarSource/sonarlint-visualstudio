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
using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    internal class ImportBeforeFileGenerator
    {
        private readonly IFileSystem fileSystem;
        private readonly ILogger logger;

        private const string targetsFileName = "SonarLint.targets";

        public ImportBeforeFileGenerator(ILogger logger) : this(logger, new FileSystem()) { }

        public ImportBeforeFileGenerator(ILogger logger, IFileSystem fileSystem)
        {
            this.logger = logger;
            this.fileSystem = fileSystem;

            WriteTargetsFileToDiskIfNotExists();
        }

        private void WriteTargetsFileToDiskIfNotExists()
        {
            logger.LogVerbose(Resources.ImportBeforeFileGenerator_CheckingIfFileExists);

            var resource = ReadResourceFile();
            var pathToImportBefore = GetPathToImportBefore();
            var fullPath = Path.Combine(pathToImportBefore, targetsFileName);

            try
            {
                if (!fileSystem.Directory.Exists(pathToImportBefore))
                {
                    logger.LogVerbose(Resources.ImportBeforeFileGenerator_CreatingDirectory, pathToImportBefore);
                    fileSystem.Directory.CreateDirectory(pathToImportBefore);
                }

                if (fileSystem.File.Exists(fullPath) && fileSystem.File.ReadAllText(fullPath) == resource)
                {
                    logger.LogVerbose(Resources.ImportBeforeFileGenerator_FileAlreadyExists);
                    return;
                }

                logger.LogVerbose(Resources.ImportBeforeFileGenerator_WritingTargetFileToDisk);
                fileSystem.File.WriteAllText(fullPath, resource);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.ImportBeforeFileGenerator_FailedToWriteFile);
                logger.LogVerbose(Resources.ImportBeforeFileGenerator_FailedToWriteFile_Verbose, ex.Message);
            }
        }

        private string ReadResourceFile()
        {
            logger.LogVerbose(Resources.ImportBeforeFileGenerator_LoadingResourceFile);

            var resourcePath = "SonarLint.VisualStudio.ConnectedMode.Embedded.SonarLintTargets.txt";

            using (var stream = GetType().Assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var data = reader.ReadToEnd();
                        return data;
                    }
                }
            }

            return "";
        }

        private string GetPathToImportBefore()
        {
            var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            var pathToImportBefore = Path.Combine(localAppData, "Microsoft", "MSBuild", "Current", "Microsoft.Common.targets", "ImportBefore");

            return pathToImportBefore;
        }
    }
}
