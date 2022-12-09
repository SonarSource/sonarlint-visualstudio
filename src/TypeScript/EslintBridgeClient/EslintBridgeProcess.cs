﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Core.JsTs;
using SonarLint.VisualStudio.Integration;

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

        /// <summary>
        /// Returns true/false if the process is running.
        /// </summary>
        bool IsRunning { get; }
    }

    [Serializable]
    internal class EslintBridgeProcessLaunchException : Exception
    {
        public EslintBridgeProcessLaunchException(string message) : base(message) { }
        public EslintBridgeProcessLaunchException(string message, Exception inner) : base(message, inner) { }
        protected EslintBridgeProcessLaunchException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    internal sealed class EslintBridgeProcess : IEslintBridgeProcess
    {
        private static readonly object Lock = new object();

        private readonly string eslintBridgeStartupScriptPath;
        private readonly ICompatibleNodeLocator compatibleNodeLocator;
        private readonly IFileSystem fileSystem;
        private readonly ILogger logger;
        private TaskCompletionSource<int> startTask;

        internal Process Process;

        private const int NO_PROCESS = 0;
        int processId = NO_PROCESS;

        internal EslintBridgeProcess(string eslintBridgeStartupScriptPath,
            ICompatibleNodeLocator compatibleNodeLocator,
            ILogger logger)
            : this(eslintBridgeStartupScriptPath, compatibleNodeLocator, new FileSystem(), logger)
        {
        }

        internal EslintBridgeProcess(string eslintBridgeStartupScriptPath,
            ICompatibleNodeLocator compatibleNodeLocator,
            IFileSystem fileSystem,
            ILogger logger)
        {
            this.eslintBridgeStartupScriptPath = eslintBridgeStartupScriptPath;
            this.compatibleNodeLocator = compatibleNodeLocator;
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

        public bool IsRunning => processId != NO_PROCESS;

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

            Process = new Process { StartInfo = psi };
            Process.ErrorDataReceived += OnErrorDataReceived;
            Process.OutputDataReceived += OnOutputDataReceived;

            Process.Start();
            // Storing the process so we don't have to worry about checking whether Process is null when logging,
            // and so that if any messages are logged after Process is set to null we know which process it related to.
            processId = Process.Id;
            logger.WriteLine(Resources.INFO_ServerProcessId, processId);
            logger.LogDebug($"[eslint-bridge] [process id: {processId}] Server process HasExited: {Process.HasExited}.");

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
            if(sender is Process terminatedProcess)
            {
                logger.LogDebug($"[eslint-bridge] [process id: {terminatedProcess.Id}] Process exited.");
                terminatedProcess.Exited -= Process_Exited;
            }
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            logger.LogDebug($"[eslint-bridge node output] [process id: {processId}] : {e.Data}");

            var portMessage = Regex.Matches(e.Data, @"port\s+(\d+)");

            if (portMessage.Count > 0)
            {
                var portNumber = int.Parse(portMessage[0].Groups[1].Value);

                if (portNumber != 0)
                {
                    logger.WriteLine(Resources.INFO_ServerStarted, processId, portNumber);
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

            logger.LogDebug($"eslint-bridge node error] [process id: {processId}] : {e.Data}");
        }

        private void TerminateRunningProcess()
        {
            try
            {
                if (Process == null || Process.HasExited)
                {
                    return;
                }

                logger.WriteLine(Resources.INFO_TerminatingServer, processId);
                Process.Kill();
                Process.Dispose();
                logger.WriteLine(Resources.INFO_ServerTerminated, processId);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // It's possible that the process exited just between the IF and the Kill(), in which case an exception is thrown.
            }
            finally
            {
                ClearProcessData();
            }
        }

        private string FindNodePath()
        {
            var nodeVersionInfo = compatibleNodeLocator.Locate();

            if (nodeVersionInfo == null)
            {
                throw new EslintBridgeProcessLaunchException("Could not find node.exe");
            }

            return nodeVersionInfo.NodeExePath;
        }

        private string FindServerScriptFile()
        {
            if (!fileSystem.File.Exists(eslintBridgeStartupScriptPath))
            {
                throw new EslintBridgeProcessLaunchException($"Could not find eslint-bridge startup script file: {eslintBridgeStartupScriptPath}");
            }

            return GetQuotedScriptPath(eslintBridgeStartupScriptPath);
        }

        private static string GetQuotedScriptPath(string scriptPath)
        {
            const string quote = "\"";
            // Quote the script path in case there are any spaces in it.
            // See #2347

            var defaultParameters = GetDefaultParameters();


            Debug.Assert(!scriptPath.Contains(quote), "Not expecting the imported script path to be quoted");

            return quote + scriptPath + quote + defaultParameters;
        }

        private static string GetDefaultParameters()
        {
            var workDir = PathHelper.GetTempDirForTask(true , "ESLintBridge", "workdir");


            /*
            To pass the sonarlint parameter we have to pass all the parameters before 
            Commandline interface for eslintbridge is not accepting named parameters  
            port - port number on which server should listen
            host - host address on which server should listen
            workDir - working directory from SonarQube API
            shouldUseTypeScriptParserForJS - whether TypeScript parser should be used for JS code (default true, can be set to false in case of perf issues)
            sonarlint - when running in SonarLint (used to not compute metrics, highlighting, etc)
            Source: https://github.com/SonarSource/SonarJS/blob/0e83c667fc6bc1687111db9343de73f725c992ca/eslint-bridge/bin/server#L4 
            */
            return $" \"0\" \"127.0.0.1\" \"{workDir}\" \"true\" \"true\""; 
        }

        private void ClearProcessData()
        {
            Process = null;
            processId = NO_PROCESS;
        }
    }
}
