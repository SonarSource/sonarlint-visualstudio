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

using System.Runtime.ExceptionServices;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.Synchronization;

namespace SonarLint.VisualStudio.Infrastructure.VS.Initialization;

public class InitializationProcessor(
    string owner,
    IReadOnlyCollection<IRequireInitialization> dependencies,
    Func<IThreadHandling, Task> initialization,
    IAsyncLockFactory asyncLockFactory,
    IThreadHandling threadHandling,
    ILogger logger) : IInitializationProcessor
{
    private readonly IAsyncLock initializationProcessLock = asyncLockFactory.Create();
    private readonly InitializationStateManager state = new();

    public bool IsFinalized => state.InitializationState.IsInitialized;

    public Task InitializeAsync() =>
        !CheckInitialized()
            ? threadHandling.RunOnBackgroundThread(() => InitializeInternalAsync())
            : Task.CompletedTask;

    private async Task InitializeInternalAsync()
    {
        using (await initializationProcessLock.AcquireAsync())
        {
            if (CheckInitialized())
            {
                return;
            }

            await DoInitializationAsync();
        }
    }

    private async Task DoInitializationAsync()
    {
        var loggerContext = new MessageLevelContext { VerboseContext = [owner] };
        try
        {
            var initialThread = threadHandling.CheckAccess();
            logger.LogVerbose(loggerContext, Resources.InitializationProcessor_Start);
            await Task.WhenAll(dependencies.Select(x => x.InitializeAsync()));
            await initialization(threadHandling);
            Debug.Assert(initialThread == threadHandling.CheckAccess(), "Thread switching should not happen");

            state.InitializationState = InitializationState.Success();
            logger.LogVerbose(loggerContext, Resources.InitializationProcessor_Finish);
        }
        catch (Exception e)
        {
            logger.WriteLine(loggerContext, e.ToString());
            state.InitializationState = InitializationState.Failure(ExceptionDispatchInfo.Capture(e));
            throw;
        }
    }

    private bool CheckInitialized()
    {
        var initializationState = state.InitializationState;
        initializationState.ThrowIfFailedInitialization();
        return initializationState.IsInitialized;
    }

    private sealed class InitializationStateManager
    {
        private readonly object updateLock = new();
        private InitializationState initializationState;

        public InitializationState InitializationState
        {
            get
            {
                lock (updateLock)
                {
                    return initializationState;
                }
            }
            set
            {
                lock (updateLock)
                {
                    initializationState = value;
                }
            }
        }
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
