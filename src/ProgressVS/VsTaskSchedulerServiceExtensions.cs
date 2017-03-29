/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto: contact AT sonarsource DOT com
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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Progress.Threading
{
    public static class VsTaskSchedulerServiceExtensions
    {
        // The maximum amount of time we're allowed to run on a single idle loop.  This comes from the
        // Windows guidance on good responsiveness (50ms) minus a few milliseconds to account for overhead.
        private const int MaxMillisecondIdleTime = 45;

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "Delegate is specific to this type but needs to be used by others.")]
        public delegate bool TryGetNextItem<T>(out T data);

        /// <summary>
        /// Runs <paramref name="idleWork"/> over each item in <paramref name="workData"/> on the UI Thread during idle time.
        /// This method will keep iterating through <paramref name="workData"/> on the same idle loop while VS remains idle.
        /// Once VS is no longer idle, the method will yield control and pick up where it left off on the next idle loop.
        /// </summary>
        /// <typeparam name="T">The type of the data to be processed.</typeparam>
        /// <param name="this">The IVsTaskSchedulerService to use when scheduling the work.</param>
        /// <param name="idleWork">Action to run on idle.</param>
        /// <param name="workData">Data to process on idle.</param>
        /// <returns
        /// >A task representing all of the asynchronous work this method is performing.  When the task is marked as completed,
        /// either all of the work in workData has been processed, or the operation was canceled.
        /// </returns>
        public static Task RunOnIdle<T>(this IVsTaskSchedulerService @this, Action<T> idleWork, IEnumerable<T> workData)
        {
            return @this.RunOnIdle<T>(idleWork, workData, CancellationToken.None);
        }

        /// <summary>
        /// Runs <paramref name="idleWork"/> over each item in <paramref name="workData"/> on the UI Thread during idle time.
        /// This method will keep iterating through <paramref name="workData"/> on the same idle loop while VS remains idle.
        /// Once VS is no longer idle, the method will yield control and pick up where it left off on the next idle loop.
        /// </summary>
        /// <typeparam name="T">The type of the data to be processed.</typeparam>
        /// <param name="this">The IVsTaskSchedulerService to use when scheduling the work.</param>
        /// <param name="idleWork">Action to run on idle.</param>
        /// <param name="workData">Data to process on idle.</param>
        /// <param name="token">Cancellation token to indicate when the work is no longer needed.</param>
        /// <returns
        /// >A task representing all of the asynchronous work this method is performing.  When the task is marked as completed,
        /// either all of the work in workData has been processed, or the operation was canceled.
        /// </returns>
        public static Task RunOnIdle<T>(this IVsTaskSchedulerService @this, Action<T> idleWork, IEnumerable<T> workData, CancellationToken token)
        {
            // Check for good data
            if (idleWork == null)
            {
                throw new ArgumentNullException(nameof(idleWork));
            }

            if (workData == null)
            {
                throw new ArgumentNullException(nameof(workData));
            }

            // Make a copy of the items since we're going to be iterating over them across multiple dispatcher operations.
            T[] items = workData.ToArray();
            var adapter = new TryGetNextItemEnumerableAdapter<T>(items);

            return @this.RunOnIdle(idleWork, adapter.TryGetNextItem, token);
        }

        /// <summary>
        /// Runs <paramref name="idleWork"/> over each item in <paramref name="workQueue"/> on the UI Thread during idle time.
        /// This method will keep iterating through <paramref name="workQueue"/> on the same idle loop while VS remains idle.
        /// Once VS is no longer idle, the method will yield control and pick up where it left off on the next idle loop.
        /// </summary>
        /// <typeparam name="T">The type of the data to be processed.</typeparam>
        /// <param name="this">The IVsTaskSchedulerService to use when scheduling the work.</param>
        /// <param name="idleWork">Action to run on idle.</param>
        /// <param name="workQueue">Data to process on idle.</param>
        /// <returns
        /// >A task representing all of the asynchronous work this method is performing.  When the task is marked as completed,
        /// either all of the work in workQueue has been processed, or the operation was canceled.
        /// </returns>
        public static Task RunOnIdle<T>(this IVsTaskSchedulerService @this, Action<T> idleWork, ConcurrentQueue<T> workQueue)
        {
            return @this.RunOnIdle<T>(idleWork, workQueue, CancellationToken.None);
        }

        /// <summary>
        /// Runs <paramref name="idleWork"/> over each item in <paramref name="workQueue"/> on the UI Thread during idle time.
        /// This method will keep iterating through <paramref name="workQueue"/> on the same idle loop while VS remains idle.
        /// Once VS is no longer idle, the method will yield control and pick up where it left off on the next idle loop.
        /// </summary>
        /// <typeparam name="T">The type of the data to be processed.</typeparam>
        /// <param name="this">The IVsTaskSchedulerService to use when scheduling the work.</param>
        /// <param name="idleWork">Action to run on idle.</param>
        /// <param name="workQueue">Data to process on idle.</param>
        /// <param name="token">Cancellation token to indicate when the work is no longer needed.</param>
        /// <returns
        /// >A task representing all of the asynchronous work this method is performing.  When the task is marked as completed,
        /// either all of the work in workQueue has been processed, or the operation was canceled.
        /// </returns>
        public static Task RunOnIdle<T>(this IVsTaskSchedulerService @this, Action<T> idleWork, ConcurrentQueue<T> workQueue, CancellationToken token)
        {
            // Check for good data
            if (idleWork == null)
            {
                throw new ArgumentNullException(nameof(idleWork));
            }

            if (workQueue == null)
            {
                throw new ArgumentNullException(nameof(workQueue));
            }

            return @this.RunOnIdle(idleWork, workQueue.TryDequeue, token);
        }

        /// <summary>
        /// Runs <paramref name="idleWork"/> over each item returned from <paramref name="tryGetNextItem"/> on the UI Thread during idle time.
        /// This method will keep processing data from <paramref name="tryGetNextItem" /> on the same idle loop while VS remains idle.
        /// Once VS is no longer idle, the method will yield control and pick up where it left off on the next idle loop.
        /// </summary>
        /// <typeparam name="T">The type of the data to be processed.</typeparam>
        /// <param name="this">The IVsTaskSchedulerService to use when scheduling the work.</param>
        /// <param name="idleWork">Action to run on idle.</param>
        /// <param name="tryGetNextItem">Delegate used to get the next piece of data to process.  tryGetNextItem should return true
        /// when another item is available and false when there is no more data to process.</param>
        /// <returns>
        /// >A task representing all of the asynchronous work this method is performing.  When the task is marked as completed,
        /// either all of the work received from tryGetNextItem has been processed, or the operation was canceled.
        /// </returns>
        public static Task RunOnIdle<T>(this IVsTaskSchedulerService @this, Action<T> idleWork, TryGetNextItem<T> tryGetNextItem)
        {
            return @this.RunOnIdle<T>(idleWork, tryGetNextItem, CancellationToken.None);
        }

        /// <summary>
        /// Runs <paramref name="idleWork"/> over each item returned from <paramref name="tryGetNextItem"/> on the UI Thread during idle time.
        /// This method will keep processing data from <paramref name="tryGetNextItem" /> on the same idle loop while VS remains idle.
        /// Once VS is no longer idle, the method will yield control and pick up where it left off on the next idle loop.
        /// </summary>
        /// <typeparam name="T">The type of the data to be processed.</typeparam>
        /// <param name="this">The IVsTaskSchedulerService to use when scheduling the work.</param>
        /// <param name="idleWork">Action to run on idle.</param>
        /// <param name="tryGetNextItem">Delegate used to get the next piece of data to process.  tryGetNextItem should return true
        /// when another item is available and false when there is no more data to process.</param>
        /// <param name="token">Cancellation token to indicate when the work is no longer needed.</param>
        /// <returns>
        /// >A task representing all of the asynchronous work this method is performing.  When the task is marked as completed,
        /// either all of the work received from tryGetNextItem has been processed, or the operation was canceled.
        /// </returns>
        public static async Task RunOnIdle<T>(this IVsTaskSchedulerService @this, Action<T> idleWork, TryGetNextItem<T> tryGetNextItem, CancellationToken token)
        {
            // Check for good data
            if (idleWork == null)
            {
                throw new ArgumentNullException(nameof(idleWork));
            }

            if (tryGetNextItem == null)
            {
                throw new ArgumentNullException(nameof(tryGetNextItem));
            }

            Stopwatch stopwatch = new Stopwatch();

            T data;
            while (tryGetNextItem(out data))
            {
                token.ThrowIfCancellationRequested();

                await VsTaskLibraryHelper.CreateAndStartTask(
                    @this,
                    VsTaskRunContext.UIThreadIdlePriority,
                    VsTaskLibraryHelper.CreateTaskBody(() =>
                    {
                        stopwatch.Start();

                        do
                        {
                            // Ensure cancellation occurring between work items is respected
                            token.ThrowIfCancellationRequested();
                            idleWork(data);
                        }
                        while (stopwatch.ElapsedMilliseconds < MaxMillisecondIdleTime && tryGetNextItem(out data));

                        stopwatch.Stop();
                    }));

                stopwatch.Reset();
            }
        }

        private class TryGetNextItemEnumerableAdapter<T>
        {
            private readonly IEnumerator<T> enumerator;

            public TryGetNextItemEnumerableAdapter(IEnumerable<T> enumerable)
            {
                this.enumerator = enumerable.GetEnumerator();
            }

            public bool TryGetNextItem(out T item)
            {
                if (this.enumerator.MoveNext())
                {
                    item = this.enumerator.Current;
                    return true;
                }
                else
                {
                    this.enumerator.Dispose();

                    item = default(T);
                    return false;
                }
            }
        }
    }
}
