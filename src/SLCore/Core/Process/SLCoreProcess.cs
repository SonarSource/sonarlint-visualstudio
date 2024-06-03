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

using SonarLint.VisualStudio.SLCore.Configuration;

namespace SonarLint.VisualStudio.SLCore.Core.Process;

internal sealed class SLCoreProcess : ISLCoreProcess
{
    private readonly ISLCoreErrorLogger errorLogger;
    private readonly System.Diagnostics.Process process;
    
    public SLCoreProcess(SLCoreLaunchParameters launchParameters, ISLCoreErrorLoggerFactory slCoreErrorLoggerFactory)
    {
        var processStartInfo = new ProcessStartInfo(launchParameters.PathToExecutable, launchParameters.LaunchArguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        process = new System.Diagnostics.Process { StartInfo = processStartInfo };
        process.Start();
        
        errorLogger = slCoreErrorLoggerFactory.Create(process.StandardError);
    }
    
    public void Dispose()
    {
        process.Close();
        process.Dispose();
        errorLogger.Dispose();
    }
    
    public IJsonRpc AttachJsonRpc(IRpcDebugger rpcDebugger)
    {
        var jsonRpcWrapper = new JsonRpcWrapper(process.StandardInput.BaseStream, process.StandardOutput.BaseStream);

        rpcDebugger.SetUpDebugger(jsonRpcWrapper);
        
        return jsonRpcWrapper;
    }
}
