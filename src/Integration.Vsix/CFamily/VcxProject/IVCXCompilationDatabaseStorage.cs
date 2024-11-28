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

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

internal interface IVCXCompilationDatabaseStorage : IDisposable
{
    string CreateDatabase(IFileConfig fileConfig);
}

[Export(typeof(IVCXCompilationDatabaseStorage))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class VcxCompilationDatabaseStorage(IThreadHandling threadHandling) : IVCXCompilationDatabaseStorage
{
    private bool disposed;
    private readonly IFileSystem fileSystem = new FileSystem();
    private string workDirectoryPath = PathHelper.GetTempDirForTask(true, "VCXCD");

    public string CreateDatabase(IFileConfig fileConfig)
    {
        threadHandling.ThrowIfOnUIThread();

        var compilationDatabaseEntry = new CompilationDatabaseEntry { Directory = fileConfig.CDDirectory, Command = fileConfig.CDCommand, File = fileConfig.CDFile };
        var compilationDatabase = new[] { compilationDatabaseEntry };

        try
        {
            fileSystem.Directory.CreateDirectory(workDirectoryPath);
            var path = Path.Combine(workDirectoryPath, $"{Path.GetFileNameWithoutExtension(compilationDatabaseEntry.File)}_{compilationDatabaseEntry.File!.GetHashCode()}.json");
            fileSystem.File.WriteAllText(path, JsonConvert.SerializeObject(compilationDatabase));
            return path;
        }
        catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
        {
            //todo log
            return null;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        fileSystem.Directory.Delete(workDirectoryPath, true);
    }
}
