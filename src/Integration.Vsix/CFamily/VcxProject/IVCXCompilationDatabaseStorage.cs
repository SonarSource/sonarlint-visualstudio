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
    ICompilationDatabaseHandle CreateDatabase(
        string file,
        string directory,
        string command,
        IEnumerable<string> environment);
}

[Export(typeof(IVCXCompilationDatabaseStorage))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class VCXCompilationDatabaseStorage : IVCXCompilationDatabaseStorage
{
    private readonly string compilationDatabaseDirectoryPath = PathHelper.GetTempDirForTask(true, "VCXCD");
    private readonly IFileSystemService fileSystemService;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;
    private bool disposed;

    [ImportingConstructor]
    public VCXCompilationDatabaseStorage(IFileSystemService fileSystemService, IThreadHandling threadHandling, ILogger logger)
    {
        this.fileSystemService = fileSystemService;
        this.threadHandling = threadHandling;
        this.logger = logger;
        }

    public ICompilationDatabaseHandle CreateDatabase(
        string file,
        string directory,
        string command,
        IEnumerable<string> environment)
    {
        ThrowIfDisposed();
        threadHandling.ThrowIfOnUIThread();

        var compilationDatabaseEntry = new CompilationDatabaseEntry
        {
            Directory = directory,
            Command = command,
            File = file,
            Environment = environment
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
