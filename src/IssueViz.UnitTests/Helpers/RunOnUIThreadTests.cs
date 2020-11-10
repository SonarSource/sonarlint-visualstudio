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
    [TestClass]
    public class RunOnUIThreadTests
    {
        // These tests are slightly hacky in that they use "ThreadHelper.SetCurrentThreadAsUIThread()"
        // to fool the VS thread helper into thinking the current thread is the UI thread.
        // 
        // However, these tests are quite limited as we can only test the cases where the
        // caller is already on the main thread - we're not hacking/mocking/faking enough 
        // of the VS setup for a call to "ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()"
        // to succeed.

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

    [TestClass]
    public class RunOnUIThreadTests_Hacky
    {
        // These tests are quite hacky in that they use "ThreadHelper.SetCurrentThreadAsUIThread()",
        // and also use reflection to set private data fields in the VS ThreadHelper hierarchy.
        // As such, they will break if the underlying VS implementation changes.
        // to fool the VS thread helper into thinking the current thread is the UI thread.
        // 
        // However, we are now hacking/mocking/faking enough of the VS setup that a call to 
        // "ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()" won't fail.
        // It won't actually cause a thread transition, but we can use the fact that our
        // scheduler mock was called to detect whether a thread transition was requested.

        private Mock<IVsTaskSchedulerService2> schedulerMock;

        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();

            schedulerMock = new Mock<IVsTaskSchedulerService2>();
            SetupTaskSchedulerMock(schedulerMock);
        }

        [TestMethod]
        public void Run_StartsOnUIThread_ExecuteOnUIThread()
        {
            bool operationExecuted = false;

            // Act
            RunOnUIThread.Run(() => operationExecuted = true);

            operationExecuted.Should().BeTrue();
            CheckThreadSwitchNotRequested(schedulerMock);
        }

        [TestMethod]
        public async Task RunAsync_StartsOnUIThread_ExecuteOnUIThread()
        {
            bool operationExecuted = false;

            // Act
            await RunOnUIThread.RunAsync(() => operationExecuted = true);

            operationExecuted.Should().BeTrue();
            CheckThreadSwitchNotRequested(schedulerMock);
        }

        [TestMethod]
        public async Task Run_StartsOnBackgroundThread_ExecuteOnUIThread()
        {
            bool operationExecuted = false;

            // Act
            await Task.Run(() =>
            {
                // Now on a background thread, so expecting the VS to request a switch to the main thread
                RunOnUIThread.Run(() => operationExecuted = true);
            });

            operationExecuted.Should().BeTrue();
            CheckThreadSwitchRequested(schedulerMock);
        }

        [TestMethod]
        public async Task RunAsync_StartsOnBackgroundThread_ExecuteOnUIThread()
        {
            bool operationExecuted = false;

            // Act
            await Task.Run(async () =>
            {
                // Now on a background thread, so expecting the VS to request a switch to the main thread
                await RunOnUIThread.RunAsync(() => operationExecuted = true);
            });

            operationExecuted.Should().BeTrue();
            CheckThreadSwitchRequested(schedulerMock);
        }

        private static void SetupTaskSchedulerMock(Mock<IVsTaskSchedulerService2> schedulerMock)
        {
            var taskContext = new JoinableTaskContext(System.Threading.Thread.CurrentThread);
            schedulerMock.As<IVsTaskSchedulerService>();
            schedulerMock.Setup(x => x.GetAsyncTaskContext()).Returns(taskContext);

            // Override the static scheduler mock used by VS
            var fieldInfo = typeof(Microsoft.VisualStudio.Shell.VsTaskLibraryHelper).GetField("cachedServiceInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            fieldInfo.SetValue(null, schedulerMock.Object);
        }

        private static void CheckThreadSwitchRequested(Mock<IVsTaskSchedulerService2> schedulerMock) =>
            schedulerMock.Invocations.Should().NotBeEmpty();

        private static void CheckThreadSwitchNotRequested(Mock<IVsTaskSchedulerService2> schedulerMock) =>
            schedulerMock.Invocations.Should().BeEmpty();
    }
}
