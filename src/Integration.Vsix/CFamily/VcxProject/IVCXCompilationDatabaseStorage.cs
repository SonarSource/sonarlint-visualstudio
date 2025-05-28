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
using Newtonsoft.Json;
using SonarLint.VisualStudio.CFamily.CompilationDatabase;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Core.SystemAbstractions;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

internal interface IObsoleteVCXCompilationDatabaseStorage : IDisposable
{
    ICompilationDatabaseHandle CreateDatabase(
        string file,
        string directory,
        string command,
        IEnumerable<string> environment);
}

internal interface IVcxCompilationDatabaseStorage : IDisposable
{
    string CreateDatabase();
    void DeleteDatabase(string databasePath);
    void UpdateDatabaseEntry(string databasePath, CompilationDatabaseEntry updatedEntry);
    void RemoveDatabaseEntry(string databasePath, string entryFilePath);
}

[Export(typeof(IObsoleteVCXCompilationDatabaseStorage))]
[Export(typeof(IVcxCompilationDatabaseStorage))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class VcxCompilationDatabaseStorage : IObsoleteVCXCompilationDatabaseStorage, IVcxCompilationDatabaseStorage
{
    private readonly string compilationDatabaseDirectoryPath = PathHelper.GetTempDirForTask(true, "VCXCD");
    private readonly IFileSystemService fileSystemService;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;
    private bool disposed;

    [ImportingConstructor]
    public VcxCompilationDatabaseStorage(IFileSystemService fileSystemService, IThreadHandling threadHandling, ILogger logger)
    {
        this.fileSystemService = fileSystemService;
        this.threadHandling = threadHandling;
        this.logger = logger;
    }

    public string CreateDatabase()
    {
        ThrowIfDisposed();
        threadHandling.ThrowIfOnUIThread();

        var compilationDatabaseFullPath = GetCompilationDatabaseFullPath();

        try
        {
            fileSystemService.Directory.CreateDirectory(compilationDatabaseDirectoryPath);
            WriteDatabaseContents(compilationDatabaseFullPath, []);
            return compilationDatabaseFullPath;
        }
        catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
        {
            logger.LogVerbose(e.ToString());
            return null;
        }
    }

    public void DeleteDatabase(string databasePath)
    {
        ThrowIfDisposed();
        threadHandling.ThrowIfOnUIThread();

        try
        {
            fileSystemService.File.Delete(databasePath);
        }
        catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
        {
            logger.LogVerbose(e.ToString());
        }
    }

    public void UpdateDatabaseEntry(string databasePath, CompilationDatabaseEntry updatedEntry)
    {
        ThrowIfDisposed();
        threadHandling.ThrowIfOnUIThread();

        try
        {
            var entries = JsonConvert.DeserializeObject<List<CompilationDatabaseEntry>>(fileSystemService.File.ReadAllText(databasePath));
            AddOrReplaceEntry(updatedEntry, entries);
            WriteDatabaseContents(databasePath, entries);
        }
        catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
        {
            logger.LogVerbose(e.ToString());
        }
    }

    public void RemoveDatabaseEntry(string databasePath, string entryFilePath)
    {
        ThrowIfDisposed();
        threadHandling.ThrowIfOnUIThread();

        try
        {
            var entries = JsonConvert.DeserializeObject<List<CompilationDatabaseEntry>>(fileSystemService.File.ReadAllText(databasePath));
            var removedCount = entries.RemoveAll(x => x.File == entryFilePath);
            if (removedCount > 0)
            {
                WriteDatabaseContents(databasePath, entries);
            }
        }
        catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
        {
            logger.LogVerbose(e.ToString());
        }
    }

    ICompilationDatabaseHandle IObsoleteVCXCompilationDatabaseStorage.CreateDatabase(
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

        var compilationDatabaseFullPath = GetCompilationDatabaseFullPath();

        try
        {
            fileSystemService.Directory.CreateDirectory(compilationDatabaseDirectoryPath);
            WriteDatabaseContents(compilationDatabaseFullPath, [compilationDatabaseEntry]);
            return new TemporaryCompilationDatabaseHandle(compilationDatabaseFullPath, fileSystemService.File, logger);
        }
        catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
        {
            logger.LogVerbose(e.ToString());
            return null;
        }
    }

    private static void AddOrReplaceEntry(CompilationDatabaseEntry updatedEntry, List<CompilationDatabaseEntry> entries)
    {
        var existingEntryLocation = entries.FindIndex(x => x.File == updatedEntry.File);
        if (existingEntryLocation == -1)
        {
            entries.Add(updatedEntry);
        }
        else
        {
            entries[existingEntryLocation] = updatedEntry;
        }
    }

    private void WriteDatabaseContents(string databasePath, List<CompilationDatabaseEntry> entries) =>
        fileSystemService.File.WriteAllText(databasePath, JsonConvert.SerializeObject(entries));

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(VcxCompilationDatabaseStorage));
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
