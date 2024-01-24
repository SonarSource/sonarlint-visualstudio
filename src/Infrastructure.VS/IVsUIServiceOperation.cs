/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    /// <summary>
    /// Helper to simplify fetching and using VS services that need to
    /// be called on the UI thread
    /// </summary>
    /// <remarks>The class handles thread switching when required. All of the methods
    /// return on the type of thread that they were called on i.e.
    /// * returns on the UI thread if called from the UI thread
    /// * returns on a background thread if called from the background thread
    /// </remarks>
    public interface IVsUIServiceOperation
    {
        /// <summary>
        /// Executes the operation synchronously on the main thread
        /// </summary>
        /// <typeparam name="S">Type of the VS service to request e.g. SVsUIShell</typeparam>
        /// <typeparam name="I">Type of the interface to return e.g. IVsUIShell</typeparam>
        /// <param name="operation">The operation to perform</param>
        void Execute<S, I>(Action<I> operation) where I : class;

        /// <summary>
        /// Executes the operation asynchronously on the main thread
        /// </summary>
        /// <typeparam name="S">Type of the VS service to request e.g. SVsUIShell</typeparam>
        /// <typeparam name="I">Type of the interface to return e.g. IVsUIShell</typeparam>
        /// <param name="operation">The operation to perform</param>
        TReturn Execute<S, I, TReturn>(Func<I, TReturn> operation) where I : class;

        /// <summary>
        /// Executes the function synchronously on the main thread
        /// </summary>
        /// <typeparam name="S">Type of the VS service to request e.g. SVsUIShell</typeparam>
        /// <typeparam name="I">Type of the interface to return e.g. IVsUIShell</typeparam>
        /// <param name="operation">The function to evaluate</param>
        Task ExecuteAsync<S, I>(Action<I> operation) where I : class;

        /// <summary>
        /// Executes the function asynchronously on the main thread
        /// </summary>
        /// <typeparam name="S">Type of the VS service to request e.g. SVsUIShell</typeparam>
        /// <typeparam name="I">Type of the interface to return e.g. IVsUIShell</typeparam>
        /// <param name="operation">The function to evaluate</param>
        Task<TReturn> ExecuteAsync<S, I, TReturn>(Func<I, TReturn> operation) where I : class;
    }

    [Export(typeof(IVsUIServiceOperation))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class VsUIServiceOperation : IVsUIServiceOperation
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IThreadHandling threadHandling;

        // Minor optimisation: remember the last service we used.
        // In the common case of an always being used with the same
        // service it means the service will only be fetched once.
        private object currentService = null;

        [ImportingConstructor]
        public VsUIServiceOperation([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IThreadHandling threadHandling)
        {
            // MEF constructor -> must be free-threaded
            this.serviceProvider = serviceProvider;
            this.threadHandling = threadHandling;
        }

        public void Execute<S, I>(Action<I> operation) where I: class
        {
            threadHandling.RunOnUIThread(() =>
            {
                var svc = GetService<S, I>();
                operation(svc);
            });
        }

        public TReturn Execute<S, I, TReturn>(Func<I, TReturn> operation) where I : class
        {
            TReturn result = default;
            threadHandling.RunOnUIThread(() =>
            {
                var svc = GetService<S, I>();
                result = operation(svc);
            });

            return result;
        }

        public async Task ExecuteAsync<S, I>(Action<I> operation) where I: class
        {
            await threadHandling.RunOnUIThreadAsync(() =>
            {
                var svc = GetService<S, I>();
                operation(svc);
            });
        }

        public async Task<TReturn> ExecuteAsync<S, I, TReturn>(Func<I, TReturn> operation) where I : class
        {
            TReturn result = default;
            await threadHandling.RunOnUIThreadAsync(() =>
            {
                var svc = GetService<S, I>();
                result = operation(svc);
            });

            return result;
        }

        private I GetService<S, I>() where I : class
        {
            // If we have a version of the service already from the previous call,
            // reuse it. Otherwise, fetch the requested service.
            var svc = currentService as I ?? serviceProvider.GetService(typeof(S)) as I;
            currentService = svc;
            return svc;
        }
    }
}
