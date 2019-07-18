/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    public sealed class ClangAnalyzerProcessRunner : IClangAnalyzerProcessRunner
    {
        public const int TimeoutMs = 10 * 1000;
        public const int ErrorCode = 1;

        private readonly ILogger logger;
        private readonly string analyzerExe;

        public ClangAnalyzerProcessRunner(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.analyzerExe = FindAnalyzerExe();
            Debug.Assert(File.Exists(analyzerExe), "Unable to locate CFamily analyzer exe");
        }

        #region Public methods

        public int ExitCode { get; private set; }

        /// <summary>
        /// Runs the specified executable and returns a boolean indicating success or failure
        /// </summary>
        /// <remarks>The standard and error output will be streamed to the logger. Child processes do not inherit the env variables from the parent automatically</remarks>
        public bool Execute(string exchangeFilePath)
        {
            if (exchangeFilePath == null)
            {
                throw new ArgumentNullException(nameof(exchangeFilePath));
            }

            var psi = new ProcessStartInfo()
            {
                FileName = analyzerExe,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false, // required if we want to capture the error output
                ErrorDialog = false,
                CreateNoWindow = true,
                Arguments = "\"" + exchangeFilePath + "\""
            };

            bool succeeded;
            using (var process = new Process())
            {
                process.StartInfo = psi;
                process.ErrorDataReceived += OnErrorDataReceived;
                process.OutputDataReceived += OnOutputDataReceived;

                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                succeeded = process.WaitForExit(TimeoutMs);
                if (succeeded)
                {
                    process.WaitForExit(); // Give any asynchronous events the chance to complete
                }

                // false means we asked the process to stop but it didn't.
                // true: we might still have timed out, but the process ended when we asked it to
                if (succeeded)
                {
                    ExitCode = process.ExitCode;
                }
                else
                {
                    ExitCode = ErrorCode;

                    try
                    {
                        process.Kill();
                        logger.WriteLine($"WARN: Execution of the Clang analyzer timed out. Killed after {TimeoutMs}");
                    }
                    catch
                    {
                        logger.WriteLine($"WARN: Execution of the Clang analyzer timed out after {TimeoutMs}. Unable to kill.");
                    }
                }

                succeeded = succeeded && (ExitCode == 0);
            }

            return succeeded;
        }

        #endregion Public methods

        #region Private methods

        private string FindAnalyzerExe()
        {
            var extensionFolder = Path.GetDirectoryName(typeof(CFamilyHelper).Assembly.Location);
            return Path.Combine(extensionFolder, "clang-0.0.4-win", "subprocess.exe");
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                logger.WriteLine("stdout: " + e.Data);
            }
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                logger.WriteLine("stderr: " + e.Data);
            }
        }

        #endregion Private methods
    }
}
