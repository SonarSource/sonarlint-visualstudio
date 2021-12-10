/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using static Microsoft.VisualStudio.Threading.AwaitExtensions;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    [ExcludeFromCodeCoverage] // Simple wrapper around hard-to-test VS types
    public class ThreadHandling : IThreadHandling
    {
        public bool CheckAccess() => ThreadHelper.CheckAccess();

        public void ThrowIfOnUIThread() => ThreadHelper.ThrowIfOnUIThread();

        public void ThrowIfNotOnUIThread() => ThreadHelper.ThrowIfNotOnUIThread();

        public async Task RunOnUIThread(Action op) => await VS.RunOnUIThread.RunAsync(op);

        public T Run<T>(Func<Task<T>> asyncMethod) => ThreadHelper.JoinableTaskFactory.Run(asyncMethod);

        public IAwaitableWrapper SwitchToBackgroundThread() => new TaskSchedulerAwaitableWrapper(new AwaitExtensions.TaskSchedulerAwaitable(TaskScheduler.Default));

        #region Wrappers for VS TaskSchedule awaiter/awaitable structs

        // The wrappers are pass-throughs - no additional logic

        private struct TaskSchedulerAwaitableWrapper : IAwaitableWrapper
        {
            private readonly TaskSchedulerAwaitable wrapped;

            public TaskSchedulerAwaitableWrapper(TaskSchedulerAwaitable awaitable)
            {
                wrapped = awaitable;
            }

            public IAwaiterWrapper GetAwaiter() => new TaskSchedulerAwaiterWrapper(wrapped.GetAwaiter());
        }

        private struct TaskSchedulerAwaiterWrapper : IAwaiterWrapper
        {
            private readonly TaskSchedulerAwaiter wrapped;

            public TaskSchedulerAwaiterWrapper(TaskSchedulerAwaiter tsAwaiter)
            {
                this.wrapped = tsAwaiter;
            }

            public void OnCompleted(Action continuation) => wrapped.OnCompleted(continuation);

            public bool IsCompleted => wrapped.IsCompleted;

            public void GetResult() => wrapped.GetResult();

#if VS2022
            // The awaiters in earlier versions of VS do not implement ICriticalNotifyCompletion
            public void UnsafeOnCompleted(Action continuation)
            {
                wrapped.UnsafeOnCompleted(continuation);
            }
#endif

        }

#endregion
    }
}
