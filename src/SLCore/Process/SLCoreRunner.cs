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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.SLCore.Core;

namespace SonarLint.VisualStudio.SLCore.Process
{
    [ExcludeFromCodeCoverage]
    public sealed class SLCoreRunner : IDisposable
    {
        public ISLCoreJsonRpc Rpc { get; }
        private readonly System.Diagnostics.Process process;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly JsonRpcWrapper jsonRpcWrapper;

        public SLCoreRunner(string pathToBat, string logFilePath = null, string errorLogFilePath = null, bool enableVerboseLogs = false)
        {
            FileStream logFileStream = null;
            if (!string.IsNullOrEmpty(logFilePath))
            {
                logFileStream = File.OpenWrite(logFilePath);
                
            }

            // todo: disable telemetry
            var processStartInfo = new ProcessStartInfo("cmd.exe", $@"/c {pathToBat}")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            process = new System.Diagnostics.Process { StartInfo = processStartInfo };

            process.Start();
            process
                .StandardOutput
                .ReadLine(); // needed to skip `echo Using bundled jre` since that is not a valid rpc msg. should be removed after it's fixed on the SLCore side
            
            Task.Run(() => ReadErrorLog(errorLogFilePath)).Forget();
            
            jsonRpcWrapper = new JsonRpcWrapper(process.StandardInput.BaseStream, process.StandardOutput.BaseStream);
            jsonRpcWrapper.TraceSource.Switch.Level = enableVerboseLogs? SourceLevels.Verbose : SourceLevels.Warning;
            jsonRpcWrapper.TraceSource.Listeners.Add(logFileStream == null ? new ConsoleTraceListener(): new TextWriterTraceListener(logFileStream));
            
            Rpc = new SLCoreJsonRpc(jsonRpcWrapper);
        }

        private void ReadErrorLog(string errorLogFilePath)
        {
            var token = cancellationTokenSource.Token;
            var fileLoggingEnabled = errorLogFilePath != null;
            var prefix = fileLoggingEnabled ? string.Empty : "ERR: ";
            StreamWriter fileStream = fileLoggingEnabled ? new StreamWriter(File.OpenWrite(errorLogFilePath)) : null;
            
            
            while (!token.IsCancellationRequested)
            {
                var line = process.StandardError.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (fileLoggingEnabled)
                {
                    fileStream.WriteLine(line);
                }
                else
                {
                    Console.WriteLine(prefix + line);
                }
            }
        }
        
        public void Dispose()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            jsonRpcWrapper?.Dispose(); // todo: slcore should be shutdown already
            process?.Dispose();
        }
    }
}
