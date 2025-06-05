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
using SonarLint.VisualStudio.CFamily.CompilationDatabase;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Synchronization;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

[Export(typeof(IActiveVcxCompilationDatabase))]
[Export(typeof(IVcxCompilationDatabaseUpdater))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal sealed class ActiveVcxCompilationDatabase(
    IVcxCompilationDatabaseStorage storage,
    IThreadHandling threadHandling,
    ICompilationDatabaseEntryGenerator generator,
    IAsyncLockFactory asyncLockFactory)
    : IActiveVcxCompilationDatabase
{
    private readonly IAsyncLock asyncLock = asyncLockFactory.Create();
    private string databasePath;
    private bool disposed = false;

    public string DatabasePath
    {
        get
        {
            ThrowIfDisposed();

            threadHandling.ThrowIfOnUIThread();

            using (asyncLock.Acquire())
            {
                return databasePath;
            }
        }
    }

    public Task<string> EnsureDatabaseInitializedAsync() =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            ThrowIfDisposed();

            using (await asyncLock.AcquireAsync())
            {
                return EnsureDatabaseInitializedInternal();
            }
        });

    public Task DropDatabaseAsync() =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            ThrowIfDisposed();

            using (await asyncLock.AcquireAsync())
            {
                if (databasePath is null)
                {
                    return;
                }
                storage.DeleteDatabase(databasePath);
                databasePath = null;
            }
        });

    public Task AddFileAsync(string filePath) =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            ThrowIfDisposed();

            using (await asyncLock.AcquireAsync())
            {
                CompilationDatabaseEntry compilationDatabaseEntry = null;
                await threadHandling.RunOnUIThreadAsync(() => compilationDatabaseEntry = generator.CreateOrNull(filePath));

                if (compilationDatabaseEntry is null)
                {
                    return;
                }

                storage.UpdateDatabaseEntry(EnsureDatabaseInitializedInternal(), compilationDatabaseEntry);
            }
        });

    public Task RemoveFileAsync(string filePath) =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            ThrowIfDisposed();

            using (await asyncLock.AcquireAsync())
            {
                if (databasePath is null)
                {
                    return;
                }

                storage.RemoveDatabaseEntry(databasePath, filePath);
            }
        });

    private string EnsureDatabaseInitializedInternal()
    {
        if (databasePath is not null)
        {
            return databasePath;
        }

        databasePath = storage.CreateDatabase();
        return databasePath;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ActiveVcxCompilationDatabase));
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        asyncLock.Dispose();
        storage.Dispose();
        databasePath = null;
        disposed = true;
    }
}
