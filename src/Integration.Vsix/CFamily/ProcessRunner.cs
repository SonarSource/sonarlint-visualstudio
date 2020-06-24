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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    /// <summary>
    /// Helper class to run an executable and capture the output
    /// </summary>
    public sealed class ProcessRunner : IProcessRunner
    {
        public const int ErrorCode = 1;

        private readonly ILogger logger;
        private readonly ISonarLintSettings settings;

        public ProcessRunner(ISonarLintSettings settings, ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public int ExitCode { get; private set; }

        /// <summary>
        /// Runs the specified executable and communicates with it via Standard IO streams.
        /// The method blocks until the handler has read to the end of the output stream, or when the cancellation token is cancelled.
        /// </summary>
        /// <remarks>
        /// Child processes do not inherit the env variables from the parent automatically.
        /// The stream reader callbacks are executed on the original calling thread.
        /// Errors and timeouts are written to the logger, which in turn writes to the output window. The caller won't see them and has no way of checking the outcome.
        /// </remarks>
        public void Execute(ProcessRunnerArguments runnerArgs)
        {
            if (runnerArgs == null)
            {
                throw new ArgumentNullException(nameof(runnerArgs));
            }

            Debug.Assert(!string.IsNullOrWhiteSpace(runnerArgs.ExeName),
                "Process runner exe name should not be null/empty");

            if (!File.Exists(runnerArgs.ExeName))
            {
                LogError(CFamilyStrings.ERROR_ProcessRunner_ExeNotFound, runnerArgs.ExeName);
                ExitCode = ErrorCode;
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = runnerArgs.ExeName,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false, // required if we want to capture the error output
                ErrorDialog = false,
                CreateNoWindow = true,
                Arguments = runnerArgs.GetEscapedArguments(),
                WorkingDirectory = runnerArgs.WorkingDirectory
            };

            SetEnvironmentVariables(psi, runnerArgs.EnvironmentVariables);

            var hasProcessStarted = false;
            var isRunningProcessCancelled = false;

            using (var process = new Process())
            using (runnerArgs.CancellationToken.Register(() =>
            {
                LogMessage(CFamilyStrings.MSG_ExecutionCancelled);

                lock (process)
                {
                    if (!hasProcessStarted)
                    {
                        // Cancellation was requested before process started - do nothing
                        return;
                    }
                }
                // Cancellation was requested after process started - kill it
                isRunningProcessCancelled = true;
                KillProcess(process);
            }))
            {
                process.ErrorDataReceived += OnErrorDataReceived;
                process.StartInfo = psi;

                lock (process)
                {
                    if (!runnerArgs.CancellationToken.IsCancellationRequested)
                    {
                        process.Start();
                        hasProcessStarted = true;
                    }
                    else
                    {
                        LogMessage(CFamilyStrings.MSG_ExecutionCancelled);
                        return;
                    }
                }

                process.BeginErrorReadLine();

                // Warning: do not log the raw command line args as they
                // may contain sensitive data
                LogDebug(CFamilyStrings.MSG_ExecutingFile,
                    runnerArgs.ExeName,
                    runnerArgs.AsLogText(),
                    runnerArgs.WorkingDirectory,
                    "",
                    process.Id);

                try
                {
                    runnerArgs.HandleInputStream(process.StandardInput);
                    runnerArgs.HandleOutputStream(process.StandardOutput);
                }
                catch (Exception) when (isRunningProcessCancelled)
                {
                    // If a process is cancelled mid-stream, an exception will be thrown.
                }
            }
        }

        private void KillProcess(Process process)
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    LogMessage(CFamilyStrings.MSG_ExectutionCancelledKilled, process.Id);
                    process.Kill();
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // It's possible that the process exited just between the IF and the Kill(), in which case an exception is thrown.
            }
        }

        private void SetEnvironmentVariables(ProcessStartInfo psi, IDictionary<string, string> envVariables)
        {
            if (envVariables == null)
            {
                return;
            }

            foreach (var envVariable in envVariables)
            {
                Debug.Assert(!string.IsNullOrEmpty(envVariable.Key), "Env variable name cannot be null or empty");

                if (psi.EnvironmentVariables.ContainsKey(envVariable.Key))
                {
                    LogDebug(CFamilyStrings.MSG_Runner_OverwritingEnvVar, envVariable.Key, psi.EnvironmentVariables[envVariable.Key], envVariable.Value);
                }
                else
                {
                    LogDebug(CFamilyStrings.MSG_Runner_SettingEnvVar, envVariable.Key, envVariable.Value);
                }
                psi.EnvironmentVariables[envVariable.Key] = envVariable.Value;
            }
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                LogError(e.Data);
            }
        }

        private void LogMessage(string message, params object[] args)
        {
            var formattedMessage = GetFormattedMessage(message, args);
            logger.WriteLine(formattedMessage);
        }

        private void LogError(string message, params object[] args)
        {
            LogMessage(CFamilyStrings.MSG_Prefix_ERROR + message, args);
        }

        private void LogDebug(string message, params object[] args)
        {
            if (settings.DaemonLogLevel == DaemonLogLevel.Verbose)
            {
                LogMessage(CFamilyStrings.MSG_Prefix_DEBUG + message, args);
            }
        }

        private static string GetFormattedMessage(string message, params object[] args)
        {
            var finalMessage = message;
            if (args != null && args.Length > 0)
            {
                finalMessage = string.Format(CultureInfo.CurrentCulture, finalMessage ?? string.Empty, args);
            }

            return finalMessage;
        }
    }
}
