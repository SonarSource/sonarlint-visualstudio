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
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    public interface IVSServiceOperation
    {
        void Execute<S, I>(Action<I> operation) where I : class;

        TReturn Execute<S, I, TReturn>(Func<I, TReturn> operation) where I : class;

        Task ExecuteAsync<S, I>(Action<I> operation) where I : class;

        Task<TReturn> ExecuteAsync<S, I, TReturn>(Func<I, TReturn> operation) where I : class;
    }

    [Export(typeof(IVSServiceOperation))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class VsServiceOperation : IVSServiceOperation
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IThreadHandling threadHandling;

        // Minor optimisation: remember the last service we used.
        // In the common case of an always being used with the same
        // service it means the service will only be fetched once.
        private object currentService = null;

        [ImportingConstructor]
        public VsServiceOperation([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
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
