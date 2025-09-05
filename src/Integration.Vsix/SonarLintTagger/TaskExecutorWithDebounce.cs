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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core.Synchronization;

namespace SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;

internal interface ITaskExecutorWithDebounceFactory
{
    ITaskExecutorWithDebounce<T> Create<T>(TimeSpan debounceMilliseconds);
}

internal interface ITaskExecutorWithDebounce<T>
{
    Task DebounceAsync(T state, Action<T> task);
}

[Export(typeof(ITaskExecutorWithDebounceFactory))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class TaskExecutorWithDebounceFactory(IAsyncLockFactory asyncLockFactory) : ITaskExecutorWithDebounceFactory
{
    public ITaskExecutorWithDebounce<T> Create<T>(TimeSpan debounceMilliseconds) => new TaskExecutorWithDebounce<T>(asyncLockFactory, debounceMilliseconds);
}

internal class TaskExecutorWithDebounce<T>(IAsyncLockFactory asyncLockFactory, TimeSpan debounceMilliseconds) : ITaskExecutorWithDebounce<T>
{
    private sealed record Debounce(CancellationTokenSource CancellationTokenSource, T State);
    private Debounce latestDebounceState;
    private readonly IAsyncLock asyncLock = asyncLockFactory.Create();

    public async Task DebounceAsync(T state, Action<T> task)
    {
        using (await asyncLock.AcquireAsync())
        {
            latestDebounceState?.CancellationTokenSource.Cancel();
            latestDebounceState = new Debounce(new CancellationTokenSource(), state);
        }

        var latestState = latestDebounceState;
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(debounceMilliseconds, latestState.CancellationTokenSource.Token);
                if (!latestState.CancellationTokenSource.Token.IsCancellationRequested)
                {
                    task(latestState.State);
                }
            }
            catch (TaskCanceledException)
            {
                // do nothing
            }
        }, latestState.CancellationTokenSource.Token).Forget();
    }
}
