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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Protocol;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class SLCoreTestProcessRunner : IDisposable
{
    public ISLCoreJsonRpc Rpc { get; }
    private readonly Process process;
    private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private readonly JsonRpcWrapper jsonRpcWrapper;
    private StreamWriter logFileStream;
    private StreamWriter errorFileStream;

    public SLCoreTestProcessRunner(string pathToBat, 
        string logFilePath = null, 
        string errorLogFilePath = null, 
        bool enableVerboseLogs = false,
        bool enableAutoFlush = false)
    {
        
        var processStartInfo = new ProcessStartInfo("cmd.exe", $@"/c {pathToBat}")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        process = new Process { StartInfo = processStartInfo };
        
        process.Start();
        SetUpLogging(logFilePath, errorLogFilePath, enableAutoFlush);
        
        jsonRpcWrapper = new JsonRpcWrapper(process.StandardInput.BaseStream, process.StandardOutput.BaseStream);
        jsonRpcWrapper.TraceSource.Switch.Level = enableVerboseLogs ? SourceLevels.Verbose : SourceLevels.Warning;
        jsonRpcWrapper.TraceSource.Listeners.Add(logFileStream == null ? new ConsoleTraceListener() : new TextWriterTraceListener(logFileStream));

        Rpc = new SLCoreJsonRpc(jsonRpcWrapper, new RpcMethodNameTransformer());
    }

    private void SetUpLogging(string logFilePath, string errorLogFilePath, bool enableAutoFlush)
    {
        if (!string.IsNullOrEmpty(logFilePath))
        {
            logFileStream = new StreamWriter(File.OpenWrite(logFilePath));
            logFileStream.AutoFlush = enableAutoFlush;
        }

        Task.Run(() => ReadErrorLog(errorLogFilePath, enableAutoFlush)).Forget();
    }

    private void ReadErrorLog(string errorLogFilePath, bool enableAutoFlush)
    {
        var token = cancellationTokenSource.Token;
        var fileLoggingEnabled = errorLogFilePath != null;
        var prefix = fileLoggingEnabled ? string.Empty : "ERR: ";
        errorFileStream = fileLoggingEnabled ? new StreamWriter(File.OpenWrite(errorLogFilePath)){AutoFlush = enableAutoFlush} : null;


        while (!token.IsCancellationRequested)
        {
            var line = process.StandardError.ReadLine();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (fileLoggingEnabled)
            {
                errorFileStream.WriteLine(line);
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
        jsonRpcWrapper?.Dispose();
        process?.Dispose();
        logFileStream?.Dispose();
        errorFileStream?.Dispose();
    }
}
