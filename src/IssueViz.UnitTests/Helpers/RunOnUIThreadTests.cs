/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Threading;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Helpers;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Helpers
{
    // These tests are slightly hacky in that they use "ThreadHelper.SetCurrentThreadAsUIThread()"
    // to fool the VS thread helper into thinking the current thread is the UI thread.
    // 
    // However, these tests are quite limited as we can only test the cases where the
    // caller is already on the main thread - we're not hacking/mocking/faking enough 
    // of the VS setup for a call to "ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()"
    // to succeed.
    [TestClass]
    public class RunOnUIThreadTests
    {
        [TestMethod]
        public void Run_StartsOnUIThread_ExecuteOnUIThread()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();

            var initialThread = Thread.CurrentThread.ManagedThreadId;
            int executionThreadId = -1;

            // Act
            RunOnUIThread.Run(() => executionThreadId = Thread.CurrentThread.ManagedThreadId);

            executionThreadId.Should().Be(initialThread);
            Thread.CurrentThread.ManagedThreadId.Should().Be(initialThread); // should still be on the "UI" thread
        }

        [TestMethod]
        public async Task RunAsync_StartsOnUIThread_ExecuteOnUIThread()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();

            var initialThread = Thread.CurrentThread.ManagedThreadId;
            int executionThreadId = -1;

            // Act
            await RunOnUIThread.RunAsync(() => executionThreadId = Thread.CurrentThread.ManagedThreadId);

            executionThreadId.Should().Be(initialThread);
            Thread.CurrentThread.ManagedThreadId.Should().Be(initialThread); // should still be on the "UI" thread
        }
    }

    // These tests are quite hacky in that they use "ThreadHelper.SetCurrentThreadAsUIThread()",
    // and also use reflection to set private data fields in the VS ThreadHelper hierarchy.
    // As such, they will break if the underlying VS implementation changes.
    // 
    // However, we are now hacking/mocking/faking enough of the VS setup that a call to 
    // "ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()" won't fail.
    // It won't actually cause a thread transition, but we can use the fact that our
    // scheduler mock was called to detect whether a thread transition was requested.
    [TestClass]
    public class RunOnUIThreadTests_HackVSThreadingInternals
    {
        [TestMethod]
        public void Run_StartsOnUIThread_ExecuteOnUIThread()
        {
            bool operationExecuted = false;
            using var hackedThreadingScope = new HackedVSThreadingScope();

            // Act
            RunOnUIThread.Run(() => operationExecuted = true);

            operationExecuted.Should().BeTrue();
            hackedThreadingScope.CheckThreadSwitchNotRequested();
        }

        [TestMethod]
        public async Task RunAsync_StartsOnUIThread_ExecuteOnUIThread()
        {
            bool operationExecuted = false;
            using var hackedThreadingScope = new HackedVSThreadingScope();

            // Act
            await RunOnUIThread.RunAsync(() => operationExecuted = true);

            operationExecuted.Should().BeTrue();
            hackedThreadingScope.CheckThreadSwitchNotRequested();
        }

        [TestMethod]
        public async Task Run_StartsOnBackgroundThread_ExecuteOnUIThread()
        {
            bool operationExecuted = false;
            using var hackedThreadingScope = new HackedVSThreadingScope();

            // Act
            await Task.Run(() =>
            {
                // Now on a background thread, so expecting the VS to request a switch to the main thread
                RunOnUIThread.Run(() => operationExecuted = true);
            });

            operationExecuted.Should().BeTrue();
            hackedThreadingScope.CheckThreadSwitchRequested();
        }

        [TestMethod]
        public async Task RunAsync_StartsOnBackgroundThread_ExecuteOnUIThread()
        {
            bool operationExecuted = false;
            using var hackedThreadingScope = new HackedVSThreadingScope();

            // Act
            await Task.Run(async () =>
            {
                // Now on a background thread, so expecting the VS to request a switch to the main thread
                await RunOnUIThread.RunAsync(() => operationExecuted = true);
            });

            operationExecuted.Should().BeTrue();
            hackedThreadingScope.CheckThreadSwitchRequested();
        }

        /// <summary>
        /// Hacks VS threading internals so that calls to ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync
        /// won't fail. This part of the hack is cleared when the scope is disposed.
        /// 
        /// Also sets the calling thread as the UI thread.
        /// This is NOT changed when the scope is disposed.
        /// </summary>
        private sealed class HackedVSThreadingScope : IDisposable
        {
            private Mock<IVsTaskSchedulerService2> schedulerMock;
            private bool disposedValue;

            private readonly System.Reflection.FieldInfo cachedServiceInstanceField;
            private object originalCachedValue;

            public HackedVSThreadingScope()
            {
                // We can't currently unset the UI thread once we've set it, so it's
                // safe for each thread to explicitly set what is considered as the UI thread.
                ThreadHelper.SetCurrentThreadAsUIThread();

                cachedServiceInstanceField = typeof(Microsoft.VisualStudio.Shell.VsTaskLibraryHelper)
                    .GetField("cachedServiceInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (cachedServiceInstanceField == null)
                {
                    Assert.Inconclusive("Test setup error: failed to locate internal VS field via Reflection. Have the VS internals changed?");
                }

                originalCachedValue = cachedServiceInstanceField.GetValue(null);

                SetupTaskSchedulerMock();
            }

            public void CheckThreadSwitchRequested() =>
                schedulerMock.Invocations.Should().NotBeEmpty();

            public void CheckThreadSwitchNotRequested() =>
                schedulerMock.Invocations.Should().BeEmpty();

            private void SetupTaskSchedulerMock()
            {
                var taskContext = new JoinableTaskContext(Thread.CurrentThread);

                schedulerMock = new Mock<IVsTaskSchedulerService2>();
                schedulerMock.As<IVsTaskSchedulerService>();
                schedulerMock.Setup(x => x.GetAsyncTaskContext()).Returns(taskContext);

                // Override the static scheduler mock used by VS
                cachedServiceInstanceField.SetValue(null, schedulerMock.Object);
            }

            public void Dispose()
            {
                if(disposedValue)
                {
                    return;
                }

                disposedValue = true;
                cachedServiceInstanceField.SetValue(null, originalCachedValue);
                originalCachedValue = null;
            }
        }
    }
}
