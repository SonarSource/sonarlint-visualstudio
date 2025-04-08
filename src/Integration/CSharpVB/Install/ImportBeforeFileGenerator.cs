/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Composition;
using System.IO;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.CSharpVB.Install;

/// <summary>
/// Creates a .targets file in the ImportBefore directory with the contents
/// of the SonarLintTargets.xml file.
/// </summary>
public interface IImportBeforeFileGenerator
{
    Task WriteTargetsFileToDiskIfNotExistsAsync();
}

[Export(typeof(IImportBeforeFileGenerator))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class ImportBeforeFileGenerator(ILogger logger, IFileSystemService fileSystem, IThreadHandling threadHandling) : IImportBeforeFileGenerator
{
    private readonly ILogger logger = logger.ForContext(Strings.ImportsBeforeFileGeneratorLogContext);
    private const string TargetsFileName = "SonarLint.targets";
    private const string ResourcePath = "SonarLint.VisualStudio.Integration.CSharpVB.Install.SonarLintTargets.xml";

    private static readonly object Locker = new();

    public Task WriteTargetsFileToDiskIfNotExistsAsync() => threadHandling.RunOnBackgroundThread(WriteTargetsFileToDiskIfNotExists);

    internal void WriteTargetsFileToDiskIfNotExists()
    {
        lock (Locker)
        {
            var fileContent = GetTargetFileContent();
            var pathToImportBefore = GetPathToImportBefore();
            var fullPath = Path.Combine(pathToImportBefore, TargetsFileName);

            logger.LogVerbose(Strings.ImportBeforeFileGenerator_CheckingIfFileExists, fullPath);

            try
            {
                if (!fileSystem.Directory.Exists(pathToImportBefore))
                {
                    logger.LogVerbose(Strings.ImportBeforeFileGenerator_CreatingDirectory, pathToImportBefore);
                    fileSystem.Directory.CreateDirectory(pathToImportBefore);
                }

                if (fileSystem.File.Exists(fullPath) && fileSystem.File.ReadAllText(fullPath) == fileContent)
                {
                    logger.LogVerbose(Strings.ImportBeforeFileGenerator_FileAlreadyExists);
                    return;
                }

                logger.LogVerbose(Strings.ImportBeforeFileGenerator_WritingTargetFileToDisk);
                fileSystem.File.WriteAllText(fullPath, fileContent);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.ImportBeforeFileGenerator_FailedToWriteFile, ex.Message);
                logger.LogVerbose(Strings.ImportBeforeFileGenerator_FailedToWriteFile_Verbose, ex.ToString());
            }
        }
    }

    private string GetTargetFileContent()
    {
        using var stream = GetType().Assembly.GetManifestResourceStream(ResourcePath);
        if (stream == null)
        {
            return "";
        }

        using var reader = new StreamReader(stream);
        var data = reader.ReadToEnd();
        return data;
    }

    private static string GetPathToImportBefore()
    {
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        var pathToImportBefore = Path.Combine(localAppData, "Microsoft", "MSBuild", "Current", "Microsoft.Common.targets", "ImportBefore");

        return pathToImportBefore;
    }
}
