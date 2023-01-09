/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ETW;
using static Microsoft.VisualStudio.Threading.AwaitExtensions;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    [ExcludeFromCodeCoverage] // Simple wrapper around hard-to-test VS types
    [Export(typeof(IThreadHandling))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ThreadHandling : IThreadHandling
    {
#pragma warning disable S4277
        public static readonly IThreadHandling Instance = new ThreadHandling();
#pragma warning restore S4277   

        public bool CheckAccess() => ThreadHelper.CheckAccess();

        public void ThrowIfOnUIThread() => ThreadHelper.ThrowIfOnUIThread();

        public void ThrowIfNotOnUIThread() => ThreadHelper.ThrowIfNotOnUIThread();

        public async Task RunOnUIThread(Action op) => await VS.RunOnUIThread.RunAsync(op);

        public async Task<T> RunOnBackgroundThread<T>(Func<Task<T>> asyncMethod)
        {
            CodeMarkers.Instance.Threading_RunOnBackgroundThread_Start();

            var startedOnUIThread = ThreadHelper.CheckAccess();

            return await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    if (startedOnUIThread)
                    {
                        CodeMarkers.Instance.Threading_RunOnBackgroundThread_SwitchingToBackground();
                        await SwitchToBackgroundThread();
                    }

                    CodeMarkers.Instance.Threading_RunOnBackgroundThread_ExecutingCall();
                    var result = await asyncMethod();

                    if (startedOnUIThread)
                    {
                        CodeMarkers.Instance.Threading_RunOnBackgroundThread_SwitchingToUI();
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    }

                    CodeMarkers.Instance.Threading_RunOnBackgroundThread_Stop();
                    return result;
                });
        }

        public T Run<T>(Func<Task<T>> asyncMethod) => ThreadHelper.JoinableTaskFactory.Run(asyncMethod);

        public async Task RunAsync<T>(Func<Task<T>> asyncMethod) => await ThreadHelper.JoinableTaskFactory.RunAsync(asyncMethod);

        public async Task RunAsync(Func<Task> asyncMethod) => await ThreadHelper.JoinableTaskFactory.RunAsync(asyncMethod);

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
