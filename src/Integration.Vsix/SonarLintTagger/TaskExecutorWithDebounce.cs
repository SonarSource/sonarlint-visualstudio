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
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;

internal interface ITaskExecutorWithDebounceFactory
{
    ITaskExecutorWithDebounce Create();
}

internal interface ITaskExecutorWithDebounce : IDisposable
{
    void Debounce(Action<CancellationToken> action, TimeSpan debounceDuration);

    bool IsScheduled { get; }
}

[Export(typeof(ITaskExecutorWithDebounceFactory))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class TaskExecutorWithDebounceFactory(IThreadHandling threadHandling) : ITaskExecutorWithDebounceFactory
{
    public ITaskExecutorWithDebounce Create() => new TaskExecutorWithDebounce(new ResettableOneShotTimer(), threadHandling);
}

internal sealed class TaskExecutorWithDebounce : ITaskExecutorWithDebounce
{
    private bool disposed;
    private readonly object locker = new();
    private readonly IThreadHandling threadHandling;
    private readonly IResettableOneShotTimer timer;
    private Action<CancellationToken> taskToExecute;
    private CancellationTokenSource lastCancellationTokenSource;

    internal TaskExecutorWithDebounce(IResettableOneShotTimer timerWrapper, IThreadHandling threadHandling)
    {
        this.threadHandling = threadHandling;
        timer = timerWrapper;
        timer.Elapsed += HandleTimerEvent;
    }

    public void Debounce(Action<CancellationToken> action, TimeSpan debounceDuration)
    {
        lock (locker)
        {
            taskToExecute = action;
            timer.Reset(debounceDuration);
        }
    }

    public bool IsScheduled
    {
        get
        {
            lock (locker)
            {
                return taskToExecute != null; // intentionally not checking the lastCancellationTokenSource, as it needs to be discarded
            }
        }
    }

    public void Dispose()
    {
        lock (locker)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            CleanUpLastExecution();
            timer.Elapsed -= HandleTimerEvent;
            timer.Dispose();
        }
    }

    private void HandleTimerEvent(object state, EventArgs eventArgs)
    {
        Action<CancellationToken> task;
        CancellationTokenSource taskCancellationTokenSource;
        lock (locker)
        {
            if (taskToExecute == null)
            {
                return;
            }

            task = taskToExecute;
            CleanUpLastExecution();
            taskCancellationTokenSource = new CancellationTokenSource();
            lastCancellationTokenSource = taskCancellationTokenSource;
        }

        Execute(task, taskCancellationTokenSource);
    }

    private void CleanUpLastExecution()
    {
        lastCancellationTokenSource?.Cancel();
        lastCancellationTokenSource?.Dispose();
        lastCancellationTokenSource = null;
        taskToExecute = null;
    }

    private void Execute(Action<CancellationToken> action, CancellationTokenSource taskCancellationTokenSource) =>
        threadHandling.RunOnBackgroundThread(() => action(taskCancellationTokenSource.Token)).Forget();
}
