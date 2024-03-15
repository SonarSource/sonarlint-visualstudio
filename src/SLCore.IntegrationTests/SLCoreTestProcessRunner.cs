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
using System.Threading;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Core.Process;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

class SLCoreTestProcessFactory : ISLCoreProcessFactory
{
    private readonly string stdErrLogPath;
    private readonly string rpcLogPath;
    private readonly bool enableVerboseLogs;
    private readonly ISLCoreProcessFactory slCoreProcessFactory;

    public SLCoreTestProcessFactory(ISLCoreProcessFactory slCoreProcessFactory, string stdErrLogPath = null, string rpcLogPath = null, bool enableVerboseLogs = false)
    {
        this.slCoreProcessFactory = slCoreProcessFactory;
        this.stdErrLogPath = stdErrLogPath;
        this.rpcLogPath = rpcLogPath;
        this.enableVerboseLogs = enableVerboseLogs;
        
    }
    
    public ISLCoreProcess StartNewProcess(SLCoreLaunchParameters slCoreLaunchParameters)
    {
        var slCoreTestProcess = new SLCoreTestProcess(slCoreProcessFactory.StartNewProcess(slCoreLaunchParameters), stdErrLogPath, rpcLogPath, enableVerboseLogs);
        return slCoreTestProcess;
    }
}

class SLCoreTestProcess : ISLCoreProcess
{
    private readonly string stdErrLogPath;
    private readonly bool enableVerboseLogs;
    private readonly ISLCoreProcess slCoreProcess;
    private readonly StreamWriter logFileStream;
    private readonly CancellationTokenSource errorLogReaderCancellation = new CancellationTokenSource();
    private StreamWriter errorFileStream;

    public SLCoreTestProcess(ISLCoreProcess slCoreProcess, 
        string stdErrLogPath = null,
        string rpcLogFilePath = null,
        bool enableVerboseLogs = false)
    {
        this.slCoreProcess = slCoreProcess;
        this.stdErrLogPath = stdErrLogPath;
        this.enableVerboseLogs = enableVerboseLogs;

        
        if (!string.IsNullOrEmpty(rpcLogFilePath))
        {
            logFileStream = new StreamWriter(File.OpenWrite(rpcLogFilePath));
            logFileStream.AutoFlush = true;
        }

        Task.Run(ReadErrorLog);
    }
    
    public void Dispose()
    {
        slCoreProcess.Dispose();
        errorLogReaderCancellation.Cancel();
        errorLogReaderCancellation.Dispose();
        errorFileStream.Dispose();
    }

    public StreamReader ErrorStreamReader => slCoreProcess?.ErrorStreamReader;
    
    public IJsonRpc AttachJsonRpc()
    {
        var jsonRpc = slCoreProcess.AttachJsonRpc();

        var jsonRpcWrapper = jsonRpc as JsonRpcWrapper;
        
        jsonRpcWrapper.TraceSource.Switch.Level = enableVerboseLogs ? SourceLevels.Verbose : SourceLevels.Warning;
        jsonRpcWrapper.TraceSource.Listeners.Add(logFileStream == null ? new ConsoleTraceListener() : new TextWriterTraceListener(logFileStream));

        return jsonRpc;
    }
    
    private void ReadErrorLog()
    {
        var token = errorLogReaderCancellation.Token;
        var fileLoggingEnabled = stdErrLogPath != null;
        var prefix = fileLoggingEnabled ? string.Empty : "ERR: ";
        errorFileStream = fileLoggingEnabled ? new StreamWriter(File.OpenWrite(stdErrLogPath)){AutoFlush = true} : null;
    
    
        while (!token.IsCancellationRequested)
        {
            var line = ErrorStreamReader.ReadLine();
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
}
