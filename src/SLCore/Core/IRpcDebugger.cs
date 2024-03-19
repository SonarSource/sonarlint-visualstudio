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

using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;

namespace SonarLint.VisualStudio.SLCore.Core;

public interface IRpcDebugger
{
    string LogFilePath { get; set; }
    internal void SetUpDebugger(IJsonRpc jsonRpc);
}

[Export(typeof(IRpcDebugger))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class RpcDebugger : IRpcDebugger
{
    private readonly IFileSystem fileSystem;
    public string LogFilePath { get; set; } = null;

    [ImportingConstructor]
    public RpcDebugger() : this(new FileSystem())
    {
    }

    internal /* for testing */ RpcDebugger(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }
    
    public void SetUpDebugger(IJsonRpc jsonRpc)
    {
        if (LogFilePath is null)
        {
            return;
        }

        SetUpTracer(jsonRpc);
    }
    
    private void SetUpTracer(IJsonRpc jsonRpc)
    {
        jsonRpc.TraceSource.Switch.Level = SourceLevels.Verbose;
        jsonRpc.TraceSource.Listeners.Add(new TextWriterTraceListener(new StreamWriter(fileSystem.FileStream.Create(LogFilePath, FileMode.Create)){AutoFlush = true}));
    }
}
