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

using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.CFamily.UnitTests.CMake
{
    [TestClass]
    public class VsDevCmdEnvironmentVarsProviderTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<VsDevCmdEnvironmentVarsProvider, IVsDevCmdEnvironmentProvider>(
                MefTestHelpers.CreateExport<IVsInfoService>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public async Task Get_MissingFile_ReturnsEmptySettings()
        {
            var context = new ProcessContext(installRootDir: "c:\\aaa\\bbb", batchFileExists: false);

            var actual = await context.TestSubject.GetAsync("any");
            actual.Should().BeNull();

            context.FileSystem.Verify(x => x.File.Exists("c:\\aaa\\bbb\\Common7\\Tools\\VsDevCmd.bat"), Times.Once);
        }

        [TestMethod]
        [DataRow("input-script-params")]
        [DataRow(null)]
        public async Task Get_ExpectedCommandsPassed(string scriptParams)
        {
            var context = new ProcessContext(installRootDir: "d:\\myinstalldir\\");

            // Act
            await context.TestSubject.GetAsync(scriptParams);

            context.ActualProcessStartInfo.Should().NotBeNull();
            context.ActualProcessStartInfo.FileName.Should().Be(Environment.GetEnvironmentVariable("COMSPEC"));

            context.ActualProcessStartInfo.Arguments.Should().NotBeNull();
            context.TestSubject.UniqueId.Should().NotBeNullOrEmpty();

            // Split and clean up the command string to make the individual commands easier to check
            var args = context.ActualProcessStartInfo.Arguments.Split(new string[] { " && " }, StringSplitOptions.None)
                .Select(x => x.Trim())
                .ToArray();

            args[0].Should().Be("/U /K set VSCMD_SKIP_SENDTELEMETRY=1"); // args to cmd.exe and first command in the sequence
            args[1].Should().Be($@"""d:\myinstalldir\Common7\Tools\VsDevCmd.bat"" {scriptParams}".TrimEnd()); // calculated full path to VsDevCmd.bat with additional params
            args[2].Should().Be("echo SONARLINT_BEGIN_CAPTURE " + context.TestSubject.UniqueId);
            args[3].Should().Be("set");
            args[4].Should().StartWith("echo SONARLINT_END_CAPTURE " + context.TestSubject.UniqueId);
        }

        [TestMethod]
        public async Task Get_ExpectedTimeoutDurationUsed()
        {
            var context = new ProcessContext();

            // Act
            await context.TestSubject.GetAsync("any");

            context.Process.Verify(x => x.WaitForExitAsync(30000), Times.Once);
        }

        [TestMethod]
        public async Task Get_Lifecycle_RunsToCompletion()
        {
            var context = new ProcessContext(hasExited: true);

            // Act
            await context.TestSubject.GetAsync("any");

            // Just checking the invocation order
            var invokedMembers = context.Process.Invocations.Select(x => x.Method.Name).ToArray();
            invokedMembers.Should().ContainInOrder(
                // Initialize and run
                "set_HandleOutputDataReceived",
                "BeginOutputReadLine",
                "WaitForExitAsync",

                // Cleanup
                "get_HasExited",
                "Dispose"
                );

            invokedMembers.Should().NotContain("Kill"); // should have terminated normally
        }

        [TestMethod]
        public async Task Get_Lifecycle_TimeoutOccurs_ProcessIsKilled()
        {
            var context = new ProcessContext(hasExited: false);

            // Act
            await context.TestSubject.GetAsync("any");

            var invokedMembers = context.Process.Invocations.Select(x => x.Method.Name).ToArray();
            invokedMembers.Should().ContainInOrder(
                "Kill",
                "Dispose"
                );
        }

        [TestMethod]
        public async Task Get_DataProcessing_DataOutsideMarkersIsIgnored()
        {
            ProcessContext.SimulateWorkInProcess writeOutputOp = (context) =>
            {
                context.WriteToOutput("before capture -> should be ignored");
                context.WriteToOutput("before_capture=ignore");

                context.WriteBeginCapture();
                context.WriteToOutput("key1=value1");
                context.WriteToOutput("not an env setting -> should be ignored");
                context.WriteToOutput("key2=value with spaces");
                context.WriteToOutput("anther not an env setting -> should be ignored");
                context.WriteEndCapture();

                context.WriteToOutput("after capture -> should be ignored");
                context.WriteToOutput("after_capture=ignore");

                return true; // hasExited
            };

            var context = new ProcessContext(simulatedWorkCallback: writeOutputOp);

            // Act
            var actual = await context.TestSubject.GetAsync("any");

            actual.Should().NotBeNull();
            actual.Count.Should().Be(2);
            actual.Keys.Should().BeEquivalentTo("key1", "key2");
            actual["key1"].Should().Be("value1");
            actual["key2"].Should().Be("value with spaces");
        }

        [TestMethod]
        public async Task Get_DataProcessing_TimeoutOccurs_NullReturned()
        {
            ProcessContext.SimulateWorkInProcess writeOutputOp = (context) =>
            {
                context.WriteBeginCapture();
                context.WriteToOutput("key1=value1");
                context.WriteToOutput("key2=value2");

                return false; // hasExited = false i.e. timeout
            };

            var context = new ProcessContext(simulatedWorkCallback: writeOutputOp);

            // Act
            var actual = await context.TestSubject.GetAsync("any");

            actual.Should().BeNull();
            context.Logger.AssertPartialOutputStringExists(Resources.VsDevCmd_TimedOut);
        }

        [TestMethod]
        public async Task Get_DataProcessing_NoSettingsCaptured_NullReturned()
        {
            ProcessContext.SimulateWorkInProcess writeOutputOp = (context) =>
            {
                context.WriteBeginCapture();
                context.WriteToOutput("not a valid setting");
                context.WriteEndCapture();
                return true; // hasExited = true i.e. completed successfully
            };

            var context = new ProcessContext(simulatedWorkCallback: writeOutputOp);

            // Act
            var actual = await context.TestSubject.GetAsync("any");

            actual.Should().BeNull();
            context.Logger.AssertPartialOutputStringExists(Resources.VsDevCmd_NoSettingsFound);
        }

        [TestMethod]
        public async Task Get_NonCriticalException_IsSuppressed()
        {
            var context = new ProcessContext();

            context.FileSystem.Reset();
            context.FileSystem.Setup(x => x.File.Exists(It.IsAny<string>()))
                .Throws(new InvalidCastException("thrown from test"));

            var actual = await context.TestSubject.GetAsync("any");

            actual.Should().BeNull();
        }

        [TestMethod]
        public void Get_CriticalException_IsNotSuppressed()
        {
            var context = new ProcessContext();

            context.FileSystem.Reset();
            context.FileSystem.Setup(x => x.File.Exists(It.IsAny<string>()))
                .Throws(new StackOverflowException("thrown from test"));

            Func<Task> act = () => context.TestSubject.GetAsync("any");

            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("thrown from test");
        }

        [TestMethod]
        public async Task Get_ThrowsIfOnUIThread()
        {
            var context = new ProcessContext();

            await context.TestSubject.GetAsync(null);

            context.ThreadHandling.Verify(x => x.ThrowIfOnUIThread(), Times.Once);
        }

        private class ProcessContext
        {
            /// <summary>
            /// Simulates work done in the process. The return value sets the status of
            /// Process.HasExited to indicate whether the process is still running or not.
            /// </summary>
            /// The WriteXXX methods can then be used to send output data to the test subject
            public delegate bool SimulateWorkInProcess(ProcessContext context);

            private readonly SimulateWorkInProcess simulatedWorkCallback;

            // Synchronisation objects used to control execution on a separate thread to simulate
            // work being done in the process
            private ManualResetEvent beginReadOutputCalled = new ManualResetEvent(false);
            private ManualResetEvent simulatedWorkCompleted = new ManualResetEvent(false);

            public ProcessContext(string installRootDir = "c:\\any",
                SimulateWorkInProcess simulatedWorkCallback = null,
                bool hasExited = true, // assume the process exits succesfully i.e. no timeout
                bool batchFileExists = true
                )
            {
                // Set up the basic mocks and properties
                Process = new Mock<IProcess>();

                ProcessFactory = new Mock<IProcessFactory>();
                ProcessFactory.Setup(x => x.Start(It.IsAny<ProcessStartInfo>()))
                    .Callback<ProcessStartInfo>(x => ActualProcessStartInfo = x)
                    .Returns(Process.Object);

                Logger = new TestLogger(logToConsole: true, logThreadId: true);

                SetHasExitedValue(hasExited);

                var batchFilePath = Path.Combine(installRootDir, "Common7", "Tools", "VsDevCmd.bat");
                FileSystem = new Mock<IFileSystem>();
                FileSystem.Setup(x => x.File.Exists(batchFilePath)).Returns(batchFileExists);

                // Don't set up the background thread / sync objects unless there is
                // actually simulated work to do.
                if(simulatedWorkCallback != null)
                {
                    this.simulatedWorkCallback = simulatedWorkCallback;
                    SetupSimulatedProcessThread();
                }

                ThreadHandling = new Mock<IThreadHandling>();

                // Create the test subject
                TestSubject = new VsDevCmdEnvironmentVarsProvider(CreateVsInfoService(installRootDir), ThreadHandling.Object, Logger,
                    ProcessFactory.Object, FileSystem.Object);
            }

            public VsDevCmdEnvironmentVarsProvider TestSubject { get; }

            public Mock<IProcess> Process { get; }

            public Mock<IProcessFactory> ProcessFactory { get; }

            public Mock<IFileSystem> FileSystem { get; }

            public Mock<IThreadHandling> ThreadHandling { get; }

            public TestLogger Logger { get; }

            /// <summary>
            /// Process start info instance created by the test subject
            /// </summary>
            public ProcessStartInfo ActualProcessStartInfo { get; private set; }

            public void WriteToOutput(string message)
            {
                Action<string> handler = Process.Object.HandleOutputDataReceived;
                handler?.Invoke(message);
            }

            public void WriteBeginCapture() =>
                WriteToOutput("SONARLINT_BEGIN_CAPTURE " + TestSubject.UniqueId);

            public void WriteEndCapture() =>
                WriteToOutput("SONARLINT_END_CAPTURE " + TestSubject.UniqueId);

            private static IVsInfoService CreateVsInfoService(string installRootDir)
            {
                var infoService = new Mock<IVsInfoService>();
                infoService.SetupGet(x => x.InstallRootDir).Returns(installRootDir);
                return infoService.Object;
            }

            private void SetHasExitedValue(bool value) =>
                Process.SetupGet(x => x.HasExited).Returns(value);

            private void SetupSimulatedProcessThread()
            {
                Process.SetupProperty(x => x.HandleOutputDataReceived);

                // Block the process from completing until the callback has completed
                Process.Setup(x => x.WaitForExitAsync(It.IsAny<int>()))
                    .Callback(() => simulatedWorkCompleted.WaitOne());

                // Start a new thread to simulate work inside the process
                Task.Run(() => CallbackToTestOnSeparateThread());

                // Unblock the processing thread when the consumer calls BeginOutputReadLine
                Process.Setup(x => x.BeginOutputReadLine())
                    .Callback(() => beginReadOutputCalled.Set());
            }

            private void CallbackToTestOnSeparateThread()
            {
                // Don't start the simulated workload until the consumer calls "BeginOutputReadLine".
                LogMessage("Waiting for BeginOutputReadLine to be called...");
                beginReadOutputCalled.WaitOne();
                LogMessage("BeginOutputReadLine has been be called. Starting to write output...");

                bool hasExited = simulatedWorkCallback.Invoke(this); // call back to the test
                SetHasExitedValue(hasExited);

                // Signal that we are done to unblock Process.WaitForExit
                LogMessage("Simulated work complete");
                simulatedWorkCompleted.Set();

                void LogMessage(string text)
                {
                    Console.WriteLine($"[Simulated process thread: {Thread.CurrentThread.ManagedThreadId}] {text}");
                }
            }
        }
    }
}
