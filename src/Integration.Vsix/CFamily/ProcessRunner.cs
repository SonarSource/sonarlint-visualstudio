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

// Note: copied from the S4MSB
// https://github.com/SonarSource/sonar-scanner-msbuild/blob/b28878e21cbdda9aca6bd08d90c3364cca882861/src/SonarScanner.MSBuild.Common/ProcessRunner.cs#L31

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
        /// Runs the specified executable and returns a boolean indicating success or failure
        /// </summary>
        /// <remarks>The standard and error output will be streamed to the logger. Child processes do not inherit the env variables from the parent automatically</remarks>
        public ProcessStreams Execute(ProcessRunnerArguments runnerArgs)
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
                return null;
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
                KillProcess(process);
            }))
            {
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
                        return null;
                    }
                }

                return new ProcessStreams(process);

                //var result = WaitForProcessToFinish(process, runnerArgs);
                //return result;
            }
        }

        private bool WaitForProcessToFinish(Process process, ProcessRunnerArguments runnerArgs)
        {
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            // Warning: do not log the raw command line args as they
            // may contain sensitive data
            LogDebug(CFamilyStrings.MSG_ExecutingFile,
                runnerArgs.ExeName,
                runnerArgs.AsLogText(),
                runnerArgs.WorkingDirectory,
                runnerArgs.TimeoutInMilliseconds,
                process.Id);

            var succeeded = process.WaitForExit(runnerArgs.TimeoutInMilliseconds);

            if (succeeded)
            {
                process.WaitForExit(); // Give any asynchronous events the chance to complete
            }

            // false means we asked the process to stop but it didn't.
            // true: we might still have timed out, but the process ended when we asked it to
            if (succeeded)
            {
                LogDebug(CFamilyStrings.MSG_ExecutionExitCode, process.ExitCode);
                ExitCode = process.ExitCode;

                if (process.ExitCode != 0 && !runnerArgs.CancellationToken.IsCancellationRequested)
                {
                    LogError(CFamilyStrings.ERROR_ProcessRunner_Failed, process.ExitCode);
                }
            }
            else
            {
                ExitCode = ErrorCode;

                try
                {
                    process.Kill();
                    LogWarning(CFamilyStrings.WARN_ExecutionTimedOutKilled, runnerArgs.TimeoutInMilliseconds,
                        runnerArgs.ExeName);
                }
                catch
                {
                    LogWarning(CFamilyStrings.WARN_ExecutionTimedOutNotKilled, runnerArgs.TimeoutInMilliseconds,
                        runnerArgs.ExeName);
                }
            }

            return succeeded && ExitCode == 0;
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

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                // It's important to log this as an important message because
                // this the log redirection pipeline of the child process
                LogMessage(e.Data);
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

        private void LogWarning(string message, params object[] args)
        {
            LogMessage(CFamilyStrings.MSG_Prefix_WARN + message, args);
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
