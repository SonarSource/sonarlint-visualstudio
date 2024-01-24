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
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.Install
{
    /// <summary>
    /// Creates a .targets file in the ImportBefore directory with the contents
    /// of the SonarLintTargets.xml file.
    /// </summary>
    internal interface IImportBeforeFileGenerator
    {
        void WriteTargetsFileToDiskIfNotExists();
    }

    [Export(typeof(IImportBeforeFileGenerator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ImportBeforeFileGenerator : IImportBeforeFileGenerator
    {
        private readonly IFileSystem fileSystem;
        private readonly ILogger logger;

        private const string targetsFileName = "SonarLint.targets";
        private const string resourcePath = "SonarLint.VisualStudio.ConnectedMode.Install.SonarLintTargets.xml";

        private static readonly object locker = new object();

        [ImportingConstructor]
        public ImportBeforeFileGenerator(ILogger logger) : this(logger, new FileSystem()) { }

        public /* for testing */ ImportBeforeFileGenerator(ILogger logger, IFileSystem fileSystem)
        {
            this.logger = logger;
            this.fileSystem = fileSystem;
        }

        public void WriteTargetsFileToDiskIfNotExists()
        {
            lock (locker)
            {
                var fileContent = GetTargetFileContent();
                var pathToImportBefore = GetPathToImportBefore();
                var fullPath = Path.Combine(pathToImportBefore, targetsFileName);

                logger.LogVerbose(Resources.ImportBeforeFileGenerator_CheckingIfFileExists, fullPath);

                try
                {
                    if (!fileSystem.Directory.Exists(pathToImportBefore))
                    {
                        logger.LogVerbose(Resources.ImportBeforeFileGenerator_CreatingDirectory, pathToImportBefore);
                        fileSystem.Directory.CreateDirectory(pathToImportBefore);
                    }

                    if (fileSystem.File.Exists(fullPath) && fileSystem.File.ReadAllText(fullPath) == fileContent)
                    {
                        logger.LogVerbose(Resources.ImportBeforeFileGenerator_FileAlreadyExists);
                        return;
                    }

                    logger.LogVerbose(Resources.ImportBeforeFileGenerator_WritingTargetFileToDisk);
                    fileSystem.File.WriteAllText(fullPath, fileContent);
                }
                catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                {
                    logger.WriteLine(Resources.ImportBeforeFileGenerator_FailedToWriteFile, ex.Message);
                    logger.LogVerbose(Resources.ImportBeforeFileGenerator_FailedToWriteFile_Verbose, ex.ToString());
                }
            }
        }

        private string GetTargetFileContent()
        {
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
