/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using TPL = System.Threading.Tasks;

namespace SonarLint.VisualStudio.Progress.Threading
{
    /// <summary>
    /// Helper class to deal with thread invocation
    /// </summary>
    internal static class VsThreadingHelper
    {
        /// <summary>
        /// Runs the specified action in the specified <see cref="VsTaskRunContext"/>.
        /// If context is the same as the current context, in terms of UI/non-UI thread, it will execute the operation directly instead of switching context.
        /// </summary>
        /// <param name="serviceProvider">An instance of <see cref="IServiceProvider"/>. Required.</param>
        /// <param name="context">The <see cref="VsTaskRunContext"/> in which to run the operation</param>
        /// <param name="op">The operation to run</param>
        internal static void RunInline(IServiceProvider serviceProvider, VsTaskRunContext context, Action op)
        {
            Debug.Assert(serviceProvider != null);
            Debug.Assert(op != null);

            RunInline<object>(serviceProvider, context, () =>
            {
                op();
                return null;
            }, null);
        }

        /// <summary>
        /// Runs the specified function in the specified <see cref="VsTaskRunContext"/>.
        /// If context is the same as the current context, in terms of UI/non-UI thread, it will execute the operation directly instead of switching context.
        /// </summary>
        /// <param name="serviceProvider">An instance of <see cref="IServiceProvider"/>. Required.</param>
        /// <param name="context">The <see cref="VsTaskRunContext"/> in which to run the operation</param>
        /// <param name="op">The operation to run</param>
        /// <param name="faultedResult">The result to return in case of a fault</param>
        /// <returns>The result of the operation</returns>
        internal static T RunInline<T>(IServiceProvider serviceProvider, VsTaskRunContext context, Func<T> op, T faultedResult)
        {
            return RunInContext(serviceProvider, context, op, faultedResult);
        }

        /// <summary>
        /// Executes the operation in the supplied <see cref="VsTaskRunContext"/>
        /// </summary>
        /// <param name="serviceProvider">An instance of <see cref="IServiceProvider"/>. Required.</param>
        /// <param name="context">The <see cref="VsTaskRunContext"/> in which to run the operation</param>
        /// <param name="op">The operation to run</param>
        /// <returns>An await-able object</returns>
        internal static async TPL.Task RunTask(IServiceProvider serviceProvider, VsTaskRunContext context, Action op)
        {
            await RunTask(serviceProvider, context, op, CancellationToken.None);
        }

        /// <summary>
        /// Executes the operation in the supplied <see cref="VsTaskRunContext"/>
        /// </summary>
        /// <param name="serviceProvider">An instance of <see cref="IServiceProvider"/>. Required.</param>
        /// <param name="context">The <see cref="VsTaskRunContext"/> in which to run the operation</param>
        /// <param name="op">The operation to run</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>An await-able object</returns>
        internal static async TPL.Task RunTask(IServiceProvider serviceProvider, VsTaskRunContext context, Action op, CancellationToken token)
        {
            await RunTask<object>(serviceProvider, context, () =>
            {
                op();
                return null;
            }, token);
        }

        /// <summary>
        /// Executes the cancellable operation in the supplied <see cref="VsTaskRunContext"/>. The result of the operation is of the generic method argument T.
        /// </summary>
        /// <param name="serviceProvider">An instance of <see cref="IServiceProvider"/>. Required.</param>
        /// <param name="context">The <see cref="VsTaskRunContext"/> in which to run the operation</param>
        /// <param name="op">The operation to run</param>
        /// <param name="token">Option cancellation token <see cref="CancellationToken"/></param>
        /// <returns>An await-able object that returns a result</returns>
        internal static async TPL.Task<T> RunTask<T>(IServiceProvider serviceProvider, VsTaskRunContext context, Func<T> op, CancellationToken token)
        {
            IVsTask task = CreateTask<T>(serviceProvider, context, op, token);
            task.Start();
            await task.GetAwaiter();
            Debug.Assert(!task.IsFaulted, "Not expecting to be faulted and reach this far");
            return (T)task.GetResult();
        }

        /// <summary>
        /// Executes a non-cancellable operation in the specified <see cref="VsTaskRunContext"/> and doesn't wait until it is completed
        /// </summary>
        /// <param name="serviceProvider">An instance of <see cref="IServiceProvider"/>. Required.</param>
        /// <param name="context">The <see cref="VsTaskRunContext"/> in which to run the operation</param>
        /// <param name="op">The operation to run</param>
        internal static void BeginTask(IServiceProvider serviceProvider, VsTaskRunContext context, Action op)
        {
            Debug.Assert(serviceProvider != null, "IServiceProvider is required");
            Debug.Assert(op != null, "Action is required");
            IVsTaskSchedulerService taskService = serviceProvider.GetService(typeof(SVsTaskSchedulerService)) as IVsTaskSchedulerService;
            IVsTaskBody body = VsTaskLibraryHelper.CreateTaskBody(op);
            IVsTask task = VsTaskLibraryHelper.CreateTask(taskService, context, VsTaskCreationOptions.NotCancelable, body, null);
            task.Start();
        }

        /// <summary>
        /// Creates a <see cref="IVsTask"/> in the specified <see cref="VsTaskRunContext"/>
        /// </summary>
        /// <param name="serviceProvider">An instance of <see cref="IServiceProvider"/>. Required.</param>
        /// <param name="context">The <see cref="VsTaskRunContext"/> in which to run the operation</param>
        /// <param name="op">The operation to run</param>
        /// <param name="token">Option cancellation token <see cref="CancellationToken"/></param>
        /// <returns>An await-able object that returns a result</returns>
        private static IVsTask CreateTask<T>(IServiceProvider serviceProvider, VsTaskRunContext context, Func<T> op, CancellationToken token)
        {
            Debug.Assert(serviceProvider != null, "IServiceProvider is required");
            Debug.Assert(op != null, "op is required");

            IVsTaskSchedulerService taskService = serviceProvider.GetService(typeof(SVsTaskSchedulerService)) as IVsTaskSchedulerService;
            IVsTaskBody body = VsTaskLibraryHelper.CreateTaskBody(() => (object)op());
            IVsTask task = VsTaskLibraryHelper.CreateTask(taskService, context, body);
            if (token != CancellationToken.None)
            {
                task.ApplyCancellationToken(token);
            }

            return task;
        }

        /// <summary>
        /// Will run the op immediately if the vsContext is the same as the current context, in terms of UI/non-UI thread
        /// </summary>
        /// <param name="serviceProvider">An instance of <see cref="IServiceProvider"/>. Required.</param>
        /// <param name="context">The <see cref="VsTaskRunContext"/> in which to run the operation</param>
        /// <param name="op">The operation to run</param>
        /// <param name="faultedResult">The result to return in case the operation has faulted</param>
        /// <returns>An await-able object that returns a result</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Justification = "Unjustified in this case since the real exception is the main thing we want to preserve")]
        private static T RunInContext<T>(IServiceProvider serviceProvider, VsTaskRunContext context, Func<T> op, T faultedResult)
        {
            bool executeOperationDirectly = context == VsTaskRunContext.CurrentContext || ThreadHelper.CheckAccess() == VsTaskLibraryHelper.IsUIThreadContext(context);
            if (executeOperationDirectly)
            {
                return op();
            }
            else
            {
                T result = faultedResult;
                Exception fault = null;
                IVsTask task = CreateTask<T>(serviceProvider, context, () =>
                    {
                        try
                        {
                            return result = op();
                        }
                        catch (Exception ex)
                        {
                            fault = ex;
                            throw; // VS doesn't bubble up the exception
                        }
                    }, CancellationToken.None);

                task.Start();
                task.Wait();
                Debug.Assert(!task.IsFaulted, "Not expecting any faults");
                if (fault != null)
                {
                    throw new Exception(string.Empty, fault);
                }

                return result;
            }
        }
    }
}
