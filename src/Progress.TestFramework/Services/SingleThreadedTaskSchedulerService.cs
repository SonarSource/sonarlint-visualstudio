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
using System.Reflection;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Test implementation of <see cref="IVsTaskSchedulerService"/> which runs all the tasks on the same thread as the calling code
    /// </summary>
    public class SingleThreadedTaskSchedulerService : SVsTaskSchedulerService, IVsTaskSchedulerService
    {
        private VsTaskRunContext currentContext;

        public SingleThreadedTaskSchedulerService()
        {
            this.SetCurrentThreadContextAs(VsTaskRunContext.UIThreadNormalPriority);
        }

        #region IVsTaskSchedulerService

        IVsTask IVsTaskSchedulerService.ContinueWhenAllCompleted(uint context, uint tasks, IVsTask[] dependentTasks, IVsTaskBody taskBody)
        {
            return ((IVsTaskSchedulerService)this).ContinueWhenAllCompletedEx(context, tasks, dependentTasks, 0, taskBody, null);
        }

        IVsTask IVsTaskSchedulerService.ContinueWhenAllCompletedEx(uint context, uint tasks, IVsTask[] dependentTasks, uint options, IVsTaskBody taskBody, object asyncState)
        {
            foreach (IVsTask t in dependentTasks)
            {
                if (!t.IsCompleted)
                {
                    t.Start();
                }
            }

            IVsTask task = ((IVsTaskSchedulerService)this).CreateTask(context, taskBody);
            task.Start();
            return task;
        }

        IVsTask IVsTaskSchedulerService.CreateTask(uint context, IVsTaskBody taskBody)
        {
            return ((IVsTaskSchedulerService)this).CreateTaskEx(context, 0, taskBody, null);
        }

        IVsTaskCompletionSource IVsTaskSchedulerService.CreateTaskCompletionSource()
        {
            throw new NotImplementedException();
        }

        IVsTaskCompletionSource IVsTaskSchedulerService.CreateTaskCompletionSourceEx(uint options, object asyncState)
        {
            throw new NotImplementedException();
        }

        IVsTask IVsTaskSchedulerService.CreateTaskEx(uint context, uint options, IVsTaskBody taskBody, object asyncState)
        {
            return new VsTask(this, (VsTaskRunContext)context, () =>
            {
                object result;
                taskBody.DoWork(null, 0, null, out result);
                return result;
            });
        }

        #endregion IVsTaskSchedulerService

        #region Configuration

        public void SetCurrentThreadIsUIThread(bool uiThread)
        {
            if (uiThread)
            {
                this.SetCurrentThreadContextAs(VsTaskRunContext.UIThreadNormalPriority);
            }
            else
            {
                this.SetCurrentThreadContextAs(VsTaskRunContext.BackgroundThread);
            }
        }

        #endregion Configuration

        #region Test helper

        /// <summary>
        /// Test helper to run test code in a pseudo-UI context that will let the platform code to behave nicely
        /// </summary>
        /// <param name="action">The action to run</param>
        public void RunInUIContext(Action action)
        {
            VsTaskRunContext prev = this.currentContext;
            this.SetCurrentThreadContextAs(VsTaskRunContext.UIThreadNormalPriority);
            try
            {
                action();
            }
            finally
            {
                this.SetCurrentThreadContextAs(prev);
            }
        }

        /// <summary>
        /// Simulates the current thread as the specified context when <see cref="ThreadHelper.CheckAccess"/> is used
        /// </summary>
        /// <param name="context">The context to set</param>
        private void SetCurrentThreadContextAs(VsTaskRunContext context)
        {
            if (context == VsTaskRunContext.CurrentContext)
            {
                // Don't need to change a thing
                return;
            }

            MethodInfo setUIThread = typeof(ThreadHelper).GetMethod("SetUIThread", BindingFlags.Static | BindingFlags.NonPublic);
            setUIThread.Should().NotBeNull("Cannot find ThreadHelper.SetUIThread");
            bool isUiThread = context == VsTaskRunContext.UIThreadBackgroundPriority ||
                context == VsTaskRunContext.UIThreadIdlePriority ||
                context == VsTaskRunContext.UIThreadNormalPriority ||
                context == VsTaskRunContext.UIThreadSend;
            try
            {
                if (isUiThread)
                {
                    // The single thread scheduler will using the current thread as if it was the UI thread
                    setUIThread.Invoke(null, new object[0]);
                }
                else
                {
                    // The single thread scheduler doesn't really uses other threads, so to simulate that: set the UI thread
                    // to be a thread on the thread pool (and the current thread will be the background one)
                    using (ManualResetEventSlim signal = new ManualResetEventSlim(false))
                    {
                        ThreadPool.QueueUserWorkItem(new WaitCallback(
                            (s) =>
                            {
                                setUIThread.Invoke(null, new object[0]);
                                signal.Set();
                            }));
                        signal.Wait();
                    }
                }
            }
            finally
            {
                this.currentContext = context;
            }

            isUiThread.Should().Be(ThreadHelper.CheckAccess(), "SetUIThread patching code failed");
        }

        #endregion Test helper

        private class VsTask : IVsTask
        {
            private readonly SingleThreadedTaskSchedulerService owner;
            private readonly Func<object> action;
            private readonly VsTaskRunContext context;
            private object result = null;

            public VsTask(SingleThreadedTaskSchedulerService owner, VsTaskRunContext context, Func<object> action)
            {
                owner.Should().NotBeNull();
                context.Should().NotBeNull();
                action.Should().NotBeNull();
                this.owner = owner;
                this.context = context;
                this.action = action;
            }

            public object AsyncState
            {
                get;
                set;
            }

            public string Description
            {
                get;
                set;
            }

            public bool IsCanceled
            {
                get;
                set;
            }

            public bool IsCompleted
            {
                get;
                set;
            }

            public bool IsFaulted
            {
                get;
                set;
            }

            public void AbortIfCanceled()
            {
                this.IsCompleted = true;
                this.IsFaulted = true;
            }

            public void Cancel()
            {
                this.IsCanceled = true;
            }

            public IVsTask ContinueWith(uint context, IVsTaskBody taskBody)
            {
                if (!this.IsCompleted)
                {
                    this.Start();
                }

                return this.ContinueWithEx(context, 0, taskBody, this.AsyncState);
            }

            public IVsTask ContinueWithEx(uint context, uint options, IVsTaskBody taskBody, object asyncState)
            {
                VsTask continuation = new VsTask(this.owner, (VsTaskRunContext)context, () =>
                {
                    object taskBodyResult;
                    taskBody.DoWork(null, 0, null, out taskBodyResult);
                    return taskBodyResult;
                });
                continuation.Start();
                return continuation;
            }

            public object GetResult()
            {
                return this.result;
            }

            public void Start()
            {
                VsTaskRunContext previous = this.owner.currentContext;
                try
                {
                    this.owner.SetCurrentThreadContextAs(this.context);
                    this.result = this.action();
                }
                catch
                {
                    this.IsFaulted = true;
                    throw;
                }
                finally
                {
                    this.IsCompleted = true;
                    this.owner.SetCurrentThreadContextAs(previous);
                }
            }

            public void Wait()
            {
            }

            public bool WaitEx(int millisecondsTimeout, uint options)
            {
                return true;
            }
        }
    }
}