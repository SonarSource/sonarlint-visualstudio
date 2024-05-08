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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.EslintBridgeClient
{
    [TestClass]
    public class EslintBridgeKeepAliveTests
    {
        [TestMethod]
        public void Constructor_TimerIsInitializedAndAStarted()
        {
            var timer = new Mock<ITimer>();

            var testSubject = CreateTestSubject(timer: timer.Object);

            timer.VerifySet(x => x.AutoReset = true, Times.Once);
            timer.VerifySet(x => x.Interval = It.IsAny<double>(), Times.Once);
            timer.Verify(x => x.Start(), Times.Once);
        }

        [TestMethod]
        public void Dispose_DisposesTimer()
        {
            var timer = new Mock<ITimer>();

            var testSubject = CreateTestSubject(timer: timer.Object);
            testSubject.Dispose();

            timer.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void EventRaised_KeepAliveSentOnlyIfProcessIsRunning(bool isProcessRunning)
        {
            var context = KeepAliveWorkflowContext.Create(isProcessRunning);
            context.ResetCalledMethod();

            context.RaiseTimerEvent();

            // Only expecting the keep-alive if the process is running
            if (isProcessRunning)
            {
                context.CheckOnlyExpectedMethods("Stop", "GetAsync", "Start");

                var uriArg = context.HttpWrapper.Invocations.First().Arguments.First();
                uriArg.ToString().EndsWith("/status").Should().BeTrue();
            }
            else
            {
                context.CheckOnlyExpectedMethods("Stop", "Start");
            }
        }

        [TestMethod]
        public void EventHandler_NonCriticalException_IsSuppressed()
        {
            var context = KeepAliveWorkflowContext.Create(isProcessRunning: true);
            context.ResetCalledMethod();

            context.Process.Setup(x => x.Start())
                .Throws(new InvalidOperationException("this is a test"));

            // We have to call the internal handler method here: if we just raise
            // the event, the Forget() method means event handler returns before
            // the exception is thrown.
            Func<Task> act = async () => await context.DirectlyInvokeTimerHandlerAsync();

            act.Should().NotThrow();

            // Should restart the timer
            context.CheckOnlyExpectedMethods("Stop", "Start");        
        }

        [TestMethod]
        public void EventHandler_CriticalException_IsNotSuppressed()
        {
            var exceptionThrowingMethodCalled = new ManualResetEvent(false);

            var context = KeepAliveWorkflowContext.Create(isProcessRunning: true);
            context.ResetCalledMethod();

            context.Process.Setup(x => x.Start())
                .Throws(new StackOverflowException("this is a test"));

            // We have to call the internal handler method here: if we just raise
            // the event, the Forget() method means event handler returns before
            // the exception is thrown.
            Func<Task> act = async () => await context.DirectlyInvokeTimerHandlerAsync();

            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("this is a test");

            // Should restart the timer
            context.CheckOnlyExpectedMethods("Stop", "Start");
        }

        private class KeepAliveWorkflowContext
        {
            private readonly Mock<ITimer> timer;
            private readonly List<string> calledMethods;
            private readonly EslintBridgeKeepAlive testSubject;

            public Mock<IEslintBridgeHttpWrapper> HttpWrapper { get; }

            public Mock<IEslintBridgeProcess> Process { get; }

            public static KeepAliveWorkflowContext Create(bool isProcessRunning) =>
                new KeepAliveWorkflowContext(isProcessRunning);

            private KeepAliveWorkflowContext(bool isProcessRunning)
            {
                calledMethods = new List<string>();
                
                // Set up callbacks to record when the Start, Stop, GetAsync are called methods
                this.timer= new Mock<ITimer>();
                timer.Setup(x => x.Start()).Callback(() => calledMethods.Add("Start"));
                timer.Setup(x => x.Stop()).Callback(() => calledMethods.Add("Stop"));

                HttpWrapper = new Mock<IEslintBridgeHttpWrapper>();
                HttpWrapper.Setup(x => x.GetAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).
                    Callback(() => calledMethods.Add("GetAsync"));

                Process = SetupServerProcess(isRunning: isProcessRunning);

                testSubject = CreateTestSubject(Process.Object, null, HttpWrapper.Object, timer.Object);
                calledMethods.Clear();
            }

            /// <summary>
            /// Simulates raising the timer event
            /// </summary>
            public void RaiseTimerEvent() =>
                timer.Raise(x => x.Elapsed += null, new TimerEventArgs(DateTime.UtcNow));

            /// <summary>
            /// Bypasses the timer event and directly invokes the async handler
            /// </summary>
            public async Task DirectlyInvokeTimerHandlerAsync() => await testSubject.HandleKeepAliveTimerElapsedAsync();

            public void ResetCalledMethod() => calledMethods.Clear();

            public void CheckOnlyExpectedMethods(params string[] expected) =>
                // Order is important so using Equal rather than IsEquivalentTo
                calledMethods.Should().Equal(expected);
        }

        private static EslintBridgeKeepAlive CreateTestSubject(IEslintBridgeProcess eslintBridgeProcess = null,
            ILogger logger = null,
            IEslintBridgeHttpWrapper httpWrapper = null,
            ITimer timer = null)
        {
            eslintBridgeProcess ??= SetupServerProcess().Object;
            logger ??= Mock.Of<ILogger>();
            httpWrapper ??= Mock.Of<IEslintBridgeHttpWrapper>();
            timer ??= new Mock<ITimer>().Object;

            return new EslintBridgeKeepAlive(eslintBridgeProcess, logger, httpWrapper, timer);
        }

        private static Mock<IEslintBridgeProcess> SetupServerProcess(bool isRunning = false)
        {
            var serverProcess = new Mock<IEslintBridgeProcess>();
            serverProcess.Setup(x => x.IsRunning).Returns(isRunning);

            return serverProcess;
        }
    }
}
