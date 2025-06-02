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
using SonarLint.VisualStudio.CFamily;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

[Export(typeof(IActiveVcxCompilationDatabase))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class ActiveVcxCompilationDatabase(
    IVcxCompilationDatabaseStorage storage,
    IThreadHandling threadHandling,
    ICompilationDatabaseEntryGenerator generator)
    : IActiveVcxCompilationDatabase
{
    private readonly object lockObject = new();
    private string databasePath;

    public string DatabasePath
    {
        get
        {
            threadHandling.ThrowIfOnUIThread();
            lock (lockObject)
            {
                return databasePath;
            }
        }
    }

    public string InitializeDatabase()
    {
        threadHandling.ThrowIfOnUIThread();
        lock (lockObject)
        {
            if (databasePath is not null)
            {
                throw new InvalidOperationException(CFamilyStrings.ActiveVcxCompilationDatabase_AlreadyInitialized);
            }

            databasePath = storage.CreateDatabase();
            return databasePath;
        }
    }

    public void DropDatabase()
    {
        threadHandling.ThrowIfOnUIThread();
        lock (lockObject)
        {
            if (databasePath is null)
            {
                return;
            }
            storage.DeleteDatabase(databasePath);
            databasePath = null;
        }
    }

    public void AddFile(string filePath)
    {
        threadHandling.ThrowIfOnUIThread();

        lock (lockObject)
        {
            if (databasePath is null)
            {
                throw new InvalidOperationException(CFamilyStrings.ActiveVcxCompilationDatabase_NotInitialized);
            }

            var compilationDatabaseEntry = generator.CreateOrNull(filePath);

            if (compilationDatabaseEntry is null)
            {
                return;
            }

            storage.UpdateDatabaseEntry(databasePath, compilationDatabaseEntry);
        }
    }

    public void RemoveFile(string filePath)
    {
        threadHandling.ThrowIfOnUIThread();

        lock (lockObject)
        {
            if (databasePath is null)
            {
                return;
            }

            storage.RemoveDatabaseEntry(databasePath, filePath);
        }
    }
}
