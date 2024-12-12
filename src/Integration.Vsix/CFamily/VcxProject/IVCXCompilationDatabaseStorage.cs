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

using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using Newtonsoft.Json;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Core.SystemAbstractions;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

internal interface IVCXCompilationDatabaseStorage : IDisposable
{
    ICompilationDatabaseHandle CreateDatabase(IFileConfig fileConfig);
}

[Export(typeof(IVCXCompilationDatabaseStorage))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal sealed class VCXCompilationDatabaseStorage(IFileSystemService fileSystemService, IEnvironmentVariableProvider environmentVariableProvider, IThreadHandling threadHandling, ILogger logger)
    : IVCXCompilationDatabaseStorage
{
    private bool disposed;
    private readonly string compilationDatabaseDirectoryPath = PathHelper.GetTempDirForTask(true, "VCXCD");

    private readonly ImmutableList<string> environmentVariablePairs = ImmutableList.CreateRange(environmentVariableProvider.GetEnvironmentVariables().Select(x => GetEnvVarPair(x.name, x.value)));

    private static string GetEnvVarPair(string name, string value) => $"{name}={value}";

    public ICompilationDatabaseHandle CreateDatabase(IFileConfig fileConfig)
    {
        ThrowIfDisposed();
        threadHandling.ThrowIfOnUIThread();

        var compilationDatabaseEntry = new CompilationDatabaseEntry
        {
            Directory = fileConfig.CDDirectory,
            Command = fileConfig.CDCommand,
            File = fileConfig.CDFile,
            Environment = environmentVariablePairs.Add(GetEnvVarPair("INCLUDE", fileConfig.EnvInclude))
        };
        var compilationDatabase = new[] { compilationDatabaseEntry };

        var compilationDatabaseFullPath = GetCompilationDatabaseFullPath();

        try
        {
            fileSystemService.Directory.CreateDirectory(compilationDatabaseDirectoryPath);
            fileSystemService.File.WriteAllText(compilationDatabaseFullPath, JsonConvert.SerializeObject(compilationDatabase));
            return new TemporaryCompilationDatabaseHandle(compilationDatabaseFullPath, fileSystemService.File, logger);
        }
        catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
        {
            logger.LogVerbose(e.ToString());
            return null;
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(VCXCompilationDatabaseStorage));
        }
    }

    private string GetCompilationDatabaseFullPath()
    {
        var compilationDatabaseFileName = $"{Guid.NewGuid()}.json";
        var compilationDatabaseFullPath = Path.Combine(compilationDatabaseDirectoryPath, compilationDatabaseFileName);
        return compilationDatabaseFullPath;
    }



    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;

        try
        {
            fileSystemService.Directory.Delete(compilationDatabaseDirectoryPath, true);
        }
        catch (Exception e)
        {
            logger.LogVerbose(e.ToString());
        }
    }
}
