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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Core.Process;
using SonarLint.VisualStudio.SLCore.Protocol;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class SLCoreTestProcessRunner : IDisposable
{
    private readonly string rpcLogFilePath;
    private readonly string stdErrLogFilePath;
    private readonly bool enableVerboseLogs;
    private readonly bool enableAutoFlush;
    private readonly ISLCoreProcessFactory slCoreProcessFactory = new SLCoreProcessFactory();
    private readonly SLCoreLaunchParameters launchParameters;
    private ISLCoreProcess process;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private JsonRpcWrapper jsonRpcWrapper;
    private StreamWriter logFileStream;
    private StreamWriter errorFileStream;
    
    internal ISLCoreJsonRpc Rpc { get; private set; }


    public SLCoreTestProcessRunner(string pathToBat, 
        string rpcLogFilePath = null, 
        string stdErrLogFilePath = null, 
        bool enableVerboseLogs = false,
        bool enableAutoFlush = false)
    {
        launchParameters = new SLCoreLaunchParameters("cmd.exe", $"/c {pathToBat}");
        this.rpcLogFilePath = rpcLogFilePath;
        this.stdErrLogFilePath = stdErrLogFilePath;
        this.enableVerboseLogs = enableVerboseLogs;
        this.enableAutoFlush = enableAutoFlush;
    }

    public void Start()
    {
        process = slCoreProcessFactory.StartNewProcess(launchParameters);
        jsonRpcWrapper = (JsonRpcWrapper)process.AttachJsonRpc();
        
        SetUpLogging();
        
        jsonRpcWrapper.TraceSource.Switch.Level = enableVerboseLogs ? SourceLevels.Verbose : SourceLevels.Warning;
        jsonRpcWrapper.TraceSource.Listeners.Add(logFileStream == null ? new ConsoleTraceListener() : new TextWriterTraceListener(logFileStream));

        Rpc = new SLCoreJsonRpc(jsonRpcWrapper, new RpcMethodNameTransformer());
    }

    private void SetUpLogging()
    {
        if (!string.IsNullOrEmpty(rpcLogFilePath))
        {
            logFileStream = new StreamWriter(File.OpenWrite(rpcLogFilePath));
            logFileStream.AutoFlush = enableAutoFlush;
        }

        Task.Run(ReadErrorLog).Forget();
    }

    private void ReadErrorLog()
    {
        var token = cancellationTokenSource.Token;
        var fileLoggingEnabled = stdErrLogFilePath != null;
        var prefix = fileLoggingEnabled ? string.Empty : "ERR: ";
        errorFileStream = fileLoggingEnabled ? new StreamWriter(File.OpenWrite(stdErrLogFilePath)){AutoFlush = enableAutoFlush} : null;


        while (!token.IsCancellationRequested)
        {
            var line = process.ErrorStreamReader.ReadLine();
            if (string.IsNullOrEmpty(line)) // potential problem here if it returns too often?
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
