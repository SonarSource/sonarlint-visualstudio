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
    ITaskExecutorWithDebounce Create(TimeSpan debounceTimeSpan);
}

internal interface ITaskExecutorWithDebounce : IDisposable
{
    void Debounce(Action action, TimeSpan? debounceTimeSpan = null);

    bool IsScheduled { get; }

    void Cancel();
}

[Export(typeof(ITaskExecutorWithDebounceFactory))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class TaskExecutorWithDebounceFactory(IThreadHandling threadHandling) : ITaskExecutorWithDebounceFactory
{
    public ITaskExecutorWithDebounce Create(TimeSpan debounceTimeSpan) => new TaskExecutorWithDebounce(new ResettableOneShotTimer(debounceTimeSpan), threadHandling);
}

internal sealed class TaskExecutorWithDebounce : ITaskExecutorWithDebounce
{
    private readonly object locker = new();
    private readonly IThreadHandling threadHandling;
    private readonly IResettableOneShotTimer timer;
    private Action taskToExecute;

    internal TaskExecutorWithDebounce(IResettableOneShotTimer timerWrapper, IThreadHandling threadHandling)
    {
        this.threadHandling = threadHandling;
        timer = timerWrapper;
        timer.Elapsed += HandleTimerEvent;
    }

    public void Debounce(Action action, TimeSpan? debounceTimeSpan = null)
    {
        lock (locker)
        {
            taskToExecute = action;
            timer.Reset(debounceTimeSpan);
        }
    }

    public bool IsScheduled
    {
        get
        {
            lock (locker)
            {
                return taskToExecute != null;
            }
        }
    }

    public void Cancel()
    {
        lock (locker)
        {
            taskToExecute = null;
            timer.Cancel();
        }
    }

    public void Dispose() // fix dispose
    {
        timer.Elapsed -= HandleTimerEvent;
        timer.Dispose();
    }

    private void HandleTimerEvent(object state, EventArgs eventArgs)
    {
        Action task;
        lock (locker)
        {
            task = taskToExecute;
            taskToExecute = null;
        }

        if (task != null)
        {
            Execute(task);
        }
    }

    private void Execute(Action action) => threadHandling.RunOnBackgroundThread(action).Forget();
}
