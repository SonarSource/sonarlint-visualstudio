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
using System.Runtime.ExceptionServices;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.Synchronization;

namespace SonarLint.VisualStudio.Infrastructure.VS.Initialization;

[Export(typeof(IInitializationHelper))]
[PartCreationPolicy(CreationPolicy.NonShared)]
[method: ImportingConstructor]
public class InitializationHelper(
    IAsyncLockFactory asyncLockFactory,
    IThreadHandling threadHandling,
    ILogger logger) : IInitializationHelper
{
    private readonly IAsyncLock asyncLock = asyncLockFactory.Create();

    private InitializationState state;

    public Task InitializeAsync(
        string owner,
        IReadOnlyCollection<IRequireInitialization> dependencies,
        Func<IThreadHandling, Task> initialization) =>
        !IsInitialized()
            ? threadHandling.RunOnBackgroundThread(() => InitializeInternalAsync(owner, dependencies, initialization))
            : Task.CompletedTask;

    private async Task InitializeInternalAsync(string owner, IReadOnlyCollection<IRequireInitialization> dependencies, Func<IThreadHandling, Task> initialization)
    {
        using (await asyncLock.AcquireAsync())
        {
            if (IsInitialized())
            {
                return;
            }

            await DoInitializationAsync(owner, dependencies, initialization);
        }
    }

    private async Task DoInitializationAsync(string owner, IReadOnlyCollection<IRequireInitialization> dependencies, Func<IThreadHandling, Task> initialization)
    {
        var loggerContext = new MessageLevelContext { VerboseContext = [owner] };
        try
        {
            logger.LogVerbose(loggerContext, "Starting initialization");
            await Task.WhenAll(dependencies.Select(x => x.InitializeAsync()));
            await initialization(threadHandling);
            Debug.Assert(!threadHandling.CheckAccess());

            state = InitializationState.Success();
            logger.LogVerbose(loggerContext, "Initialization complete");
        }
        catch (Exception e)
        {
            logger.WriteLine(loggerContext, e.ToString());
            state = InitializationState.Failure(ExceptionDispatchInfo.Capture(e));
            throw;
        }
    }

    private bool IsInitialized()
    {
        var initializationState = state;
        if (initializationState.IsInitialized)
        {
            initializationState.ThrowIfFailedInitialization();
            return true;
        }
        return false;
    }

    private readonly struct InitializationState
    {
        private readonly ExceptionDispatchInfo initializationException;

        public static InitializationState Success() => new(true, null);

        public static InitializationState Failure(ExceptionDispatchInfo initializationException) => new(true, initializationException);

        private InitializationState(bool isInitialized, ExceptionDispatchInfo initializationException)
        {
            IsInitialized = isInitialized;
            this.initializationException = initializationException;
        }

        public bool IsInitialized { get; }

        public void ThrowIfFailedInitialization() => initializationException?.Throw();
    }
}
