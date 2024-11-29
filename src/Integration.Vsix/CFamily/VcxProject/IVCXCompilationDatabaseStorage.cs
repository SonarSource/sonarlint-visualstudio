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

using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using Newtonsoft.Json;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Core.SystemAbstractions;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

internal interface IVCXCompilationDatabaseStorage : IDisposable
{
    string CreateDatabase(IFileConfig fileConfig);
}

[Export(typeof(IVCXCompilationDatabaseStorage))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal sealed class VCXCompilationDatabaseStorage(IFileSystemService fileSystemService, IThreadHandling threadHandling, ILogger logger)
    : IVCXCompilationDatabaseStorage
{
    private bool disposed;
    private readonly string compilationDatabaseDirectoryPath = PathHelper.GetTempDirForTask(true, "VCXCD");

    public string CreateDatabase(IFileConfig fileConfig)
    {
        ThrowIfDisposed();
        threadHandling.ThrowIfOnUIThread();

        var compilationDatabaseEntry = new CompilationDatabaseEntry
        {
            Directory = fileConfig.CDDirectory,
            Command = fileConfig.CDCommand,
            File = fileConfig.CDFile
        };
        var compilationDatabase = new[] { compilationDatabaseEntry };

        var compilationDatabaseFullPath = GetCompilationDatabaseFullPath(compilationDatabaseEntry);

        try
        {
            fileSystemService.Directory.CreateDirectory(compilationDatabaseDirectoryPath);
            fileSystemService.File.WriteAllText(compilationDatabaseFullPath, JsonConvert.SerializeObject(compilationDatabase));
            return compilationDatabaseFullPath;
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

    private string GetCompilationDatabaseFullPath(CompilationDatabaseEntry compilationDatabaseEntry)
    {
        var compilationDatabaseFileName = $"{Path.GetFileName(compilationDatabaseEntry.File)}.{compilationDatabaseEntry.File!.GetHashCode()}.json";
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
        fileSystemService.Directory.Delete(compilationDatabaseDirectoryPath, true);
    }
}
