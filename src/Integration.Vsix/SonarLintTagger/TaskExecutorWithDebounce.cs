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

internal class TaskExecutorWithDebounce : ITaskExecutorWithDebounce
{
    private Action latestDebounceState;
    private object locker = new object();
    private readonly long debounceMilliseconds;
    private readonly Timer timer;

    public TaskExecutorWithDebounce(IAsyncLockFactory asyncLockFactory, TimeSpan debounceMilliseconds)
    {
        this.debounceMilliseconds = (long)debounceMilliseconds.TotalMilliseconds;

        timer = new Timer(DebounceAction, null, (long)debounceMilliseconds.TotalMilliseconds, Timeout.Infinite);
    }

    private void DebounceAction(object state)
    {
        Action action;
        lock (locker)
        {
            action = latestDebounceState;
        }
        action?.Invoke();
    }

    public async Task DebounceAsync(Action task)
    {
        lock (locker)
        {
            latestDebounceState = task;
            timer.Change(debounceMilliseconds, Timeout.Infinite);
        }
    }
}
