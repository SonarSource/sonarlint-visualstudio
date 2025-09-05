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
    ITaskExecutorWithDebounce Create(TimeSpan debounceMilliseconds);
}

internal interface ITaskExecutorWithDebounce
{
    Task DebounceAsync(Action task);
}

[Export(typeof(ITaskExecutorWithDebounceFactory))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class TaskExecutorWithDebounceFactory(IAsyncLockFactory asyncLockFactory) : ITaskExecutorWithDebounceFactory
{
    public ITaskExecutorWithDebounce Create(TimeSpan debounceMilliseconds) => new TaskExecutorWithDebounce(asyncLockFactory, debounceMilliseconds);
}

internal class TaskExecutorWithDebounce(IAsyncLockFactory asyncLockFactory, TimeSpan debounceMilliseconds) : ITaskExecutorWithDebounce
{
    private sealed record Debounce(CancellationTokenSource CancellationTokenSource);
    private Debounce latestDebounceState;
    private readonly IAsyncLock asyncLock = asyncLockFactory.Create();

    public async Task DebounceAsync(Action task)
    {
        Debounce latestState;
        using (await asyncLock.AcquireAsync())
        {
            latestDebounceState?.CancellationTokenSource.Cancel();
            latestDebounceState = new Debounce(new CancellationTokenSource());
            latestState = latestDebounceState;
        }

        ExecuteAction(task, latestState);
    }

    private void ExecuteAction(Action task, Debounce latestState) =>
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(debounceMilliseconds, latestState.CancellationTokenSource.Token);
                if (!latestState.CancellationTokenSource.Token.IsCancellationRequested)
                {
                    task();
                }
            }
            catch (TaskCanceledException)
            {
                // do nothing
            }
        }, latestState.CancellationTokenSource.Token).Forget();
}
