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

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;

namespace SonarLint.VisualStudio.SLCore.Core;

public interface IRpcDebugger
{
    internal void SetUpDebugger(IJsonRpc jsonRpc);
}

[Export(typeof(IRpcDebugger))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class RpcDebugger : IRpcDebugger
{
    private readonly StreamWriter streamWriter;

    [ImportingConstructor]
    public RpcDebugger() : this(new FileSystem(), DateTime.Now)
    {
    }

    internal /* for testing */ RpcDebugger(IFileSystem fileSystem, DateTime fileDate) :
        this(fileSystem, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SonarLint for Visual Studio", "Rpc Logs", $"{fileDate.ToString("yyyy-MM-dd_HHmmssffff")}.log"))
    {
    }

    internal /* for testing */ RpcDebugger(IFileSystem fileSystem, string logFilePath)
    {
#if DEBUG
        /* IMPORTANT!!!
         * Enable this environment variable only if you want to collect rpc debug logs.
         * Debug logs contain sensitive data, like customer SQ/SC user and token info which potentially can be leaked. */
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SONARLINT_LOG_RPC")))
        {
            fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
            streamWriter = new StreamWriter(fileSystem.FileStream.Create(logFilePath, FileMode.Create)) { AutoFlush = true };
        }
#endif
    }

    public void SetUpDebugger(IJsonRpc jsonRpc)
    {
        
        if (streamWriter is not null)
        {
            jsonRpc.TraceSource.Switch.Level = SourceLevels.Verbose;
            jsonRpc.TraceSource.Listeners.Add(new TextWriterTraceListener(streamWriter));
        }
    }
}
