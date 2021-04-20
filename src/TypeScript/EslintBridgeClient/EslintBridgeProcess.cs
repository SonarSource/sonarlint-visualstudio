/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.TypeScript.NodeJSLocator;

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient
{
    internal interface IEslintBridgeProcess : IDisposable
    {
        /// <summary>
        /// Ensures that the process is running. Returns the port that server is running on.
        /// </summary>
        Task<int> Start();

        /// <summary>
        /// Terminates running process.
        /// </summary>
        void Stop();
    }

    [Export(typeof(IEslintBridgeProcess))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class EslintBridgeProcess : IEslintBridgeProcess
    {
        internal const string EslintBridgeDirectoryMefContractName = "SonarLint.TypeScript.EsLintBridgeServerPath";
        private static readonly object Lock = new object();

        private readonly string eslintBridgeStartupScriptPath;
        private readonly INodeLocator nodeLocator;
        private readonly IFileSystem fileSystem;
        private readonly ILogger logger;
        private TaskCompletionSource<int> startTask;

        internal Process Process;

        [ImportingConstructor]
        public EslintBridgeProcess([Import(EslintBridgeDirectoryMefContractName)] string eslintBridgeStartupScriptPath,
            INodeLocator nodeLocator,
            ILogger logger)
            : this(eslintBridgeStartupScriptPath, nodeLocator, new FileSystem(), logger)
        {
        }

        internal EslintBridgeProcess(string eslintBridgeStartupScriptPath,
            INodeLocator nodeLocator,
            IFileSystem fileSystem,
            ILogger logger)
        {
            this.eslintBridgeStartupScriptPath = eslintBridgeStartupScriptPath;
            this.nodeLocator = nodeLocator;
            this.fileSystem = fileSystem;
            this.logger = logger;
        }

        public Task<int> Start()
        {
            lock (Lock)
            {
                var shouldSpawnNewProcess = startTask == null ||
                                            startTask.Task.IsFaulted ||
                                            Process == null ||
                                            Process.HasExited;

                if (!shouldSpawnNewProcess)
                {
                    return startTask.Task;
                }
                startTask = new TaskCompletionSource<int>();

                try
                {
                    StartServer();
                }
                catch (Exception ex)
                {
                    startTask.SetException(ex);
                }

                return startTask.Task;
            }
        }

        public void Stop()
        {
            lock (Lock)
            {
                TerminateRunningProcess();
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private void StartServer()
        {
            var nodePath = FindNodePath();
            var serverStartupScriptLocation = FindServerScriptFile();

            var psi = new ProcessStartInfo
            {
                FileName = nodePath,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false, // required if we want to capture the error output
                ErrorDialog = false,
                CreateNoWindow = true,
                Arguments = serverStartupScriptLocation
            };

            Process = new Process {StartInfo = psi};
            Process.ErrorDataReceived += OnErrorDataReceived;
            Process.OutputDataReceived += OnOutputDataReceived;

            Process.Start();
            logger.WriteLine(Resources.INFO_ServerProcessId, Process.Id);
            logger.LogDebug(Resources.INFO_ServerHasExisted, Process.HasExited);

            Process.BeginErrorReadLine();
            Process.BeginOutputReadLine();

            // Note: we need to call the "BeginXXX" methods before registering to the exit event.
            // Otherwise, if the underlying process crashes immediately, the event handler could
            // have set "Process" to null before we get a chance to call the "BeginXXX" methods ->
            // we lose the error output and get a null ref exception instead.
            Process.Exited += Process_Exited;
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            Process = null;
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            logger.LogDebug("ESLINT-BRIDGE OUTPUT: " + e.Data);

            var portMessage = Regex.Matches(e.Data, @"port\s+(\d+)");

            if (portMessage.Count > 0)
            {
                var portNumber = int.Parse(portMessage[0].Groups[1].Value);

                if (portNumber != 0)
                {
                    logger.WriteLine(Resources.INFO_ServerStarted, portNumber);
                    startTask.SetResult(portNumber);
                }
            }
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            logger.LogDebug("ESLINT-BRIDGE ERROR: " + e.Data);
        }

        private void TerminateRunningProcess()
        {
            try
            {
                if (Process == null || Process.HasExited)
                {
                    logger.LogDebug(Resources.INFO_ServerAlreadyTerminated);
                }
                else
                {
                    var processId = Process.Id;
                    logger.WriteLine(Resources.INFO_TerminatingServer, processId);
                    Process.Kill();
                    Process.Dispose();
                    logger.WriteLine(Resources.INFO_ServerTerminated, processId);
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // It's possible that the process exited just between the IF and the Kill(), in which case an exception is thrown.
            }
            finally
            {
                Process = null;
            }
        }

        private string FindNodePath()
        {
            var nodePath = nodeLocator.Locate();

            if (string.IsNullOrEmpty(nodePath))
            {
                throw new FileNotFoundException("Could not find node.exe");
            }

            return nodePath;
        }

        private string FindServerScriptFile()
        {
            if (!fileSystem.File.Exists(eslintBridgeStartupScriptPath))
            {
                throw new FileNotFoundException($"Could not find eslint-bridge startup script file: {eslintBridgeStartupScriptPath}");
            }

            return GetQuotedScriptPath(eslintBridgeStartupScriptPath);
        }

        private static string GetQuotedScriptPath(string scriptPath)
        {
            const string Quote = "\"";
            // Quote the script path in case there are any spaces in it.
            // See #2347
            Debug.Assert(!scriptPath.Contains(Quote), "Not expecting the imported script path to be quoted");

            return Quote + scriptPath + Quote;

        }
    }
}
