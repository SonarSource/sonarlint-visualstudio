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
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.TestInfrastructure
{
    /// <summary>
    /// Dummy implementation of IThreadHandling
    /// </summary>
    /// <remarks>All operations are performed synchronously and return immediately.
    /// No thread switches take place.</remarks>
    public class NoOpThreadHandler : IThreadHandling
    {
        public bool CheckAccess() => true;

        public T Run<T>(Func<Task<T>> asyncMethod) => asyncMethod().Result;
        public Task RunAsync(Func<Task> asyncMethod) => asyncMethod();

        public Task RunAsync<T>(Func<Task<T>> asyncMethod) => asyncMethod();

        public Task RunOnUIThreadAsync(Action op)
        {
            op();
            return Task.CompletedTask;
        }

        public void RunOnUIThread(Action op) => op();

        public Task<T> RunOnBackgroundThread<T>(Func<Task<T>> asyncMethod) => asyncMethod();

        public IAwaitableWrapper SwitchToBackgroundThread() => new NoOpAwaitable();

        public Task SwitchToMainThreadAsync() => Task.CompletedTask;

        public void ThrowIfNotOnUIThread() { /* no-op */ }

        public void ThrowIfOnUIThread() { /* no-op */ }

        #region No-op awaiter/awaitable

        private class NoOpAwaiter : IAwaiterWrapper
        {
            public bool IsCompleted => true;

            public void GetResult() { /* no-op */ }

            public void OnCompleted(Action continuation) => continuation();

            public void UnsafeOnCompleted(Action continuation) => continuation();
        }

        public class NoOpAwaitable : IAwaitableWrapper
        {
            public IAwaiterWrapper GetAwaiter() => new NoOpAwaiter();
        }

        #endregion
    }
}
