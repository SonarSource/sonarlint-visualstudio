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
using System.Threading.Tasks;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Core.Process;

namespace SonarLint.VisualStudio.SLCore;

internal interface ISLCoreRpcFactory
{
    ISLCoreRpc StartNewRpcInstance();
}

[Export(typeof(ISLCoreRpcFactory))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class SLCoreRpcFactory : ISLCoreRpcFactory
{
    private readonly ISLCoreProcessFactory slCoreProcessFactory;
    private readonly ISLCoreLocator slCoreLocator;
    private readonly ISLCoreJsonRpcFactory slCoreJsonRpcFactory;
    private readonly IRpcDebugger rpcDebugger;
    private readonly ISLCoreServiceProviderWriter slCoreServiceProvider;
    private readonly ISLCoreListenerSetUp slCoreListenerSetUp;

    [ImportingConstructor]
    public SLCoreRpcFactory(ISLCoreProcessFactory slCoreProcessFactory,
        ISLCoreLocator slCoreLocator,
        ISLCoreJsonRpcFactory slCoreJsonRpcFactory,
        IRpcDebugger rpcDebugger,
        ISLCoreServiceProviderWriter slCoreServiceProvider,
        ISLCoreListenerSetUp slCoreListenerSetUp)
    {
        this.slCoreProcessFactory = slCoreProcessFactory;
        this.slCoreLocator = slCoreLocator;
        this.slCoreJsonRpcFactory = slCoreJsonRpcFactory;
        this.rpcDebugger = rpcDebugger;
        this.slCoreServiceProvider = slCoreServiceProvider;
        this.slCoreListenerSetUp = slCoreListenerSetUp;
    }

    public ISLCoreRpc StartNewRpcInstance() =>
        new SlCoreRpc(slCoreProcessFactory.StartNewProcess(slCoreLocator.LocateExecutable()),
            slCoreJsonRpcFactory,
            rpcDebugger,
            slCoreServiceProvider,
            slCoreListenerSetUp);
}

public interface ISLCoreRpc : IDisposable
{
    ISLCoreServiceProvider ServiceProvider { get; }

    public Task ShutdownTask { get; }
}

internal sealed class SlCoreRpc : ISLCoreRpc
{
    private readonly ISLCoreProcess slCoreProcess;

    public ISLCoreServiceProvider ServiceProvider { get; set; }
    public Task ShutdownTask { get; set; }

    public SlCoreRpc(ISLCoreProcess slCoreProcess,
        ISLCoreJsonRpcFactory slCoreJsonRpcFactory,
        IRpcDebugger rpcDebugger,
        ISLCoreServiceProviderWriter slCoreServiceProvider,
        ISLCoreListenerSetUp listenerSetUp)
    {
        this.slCoreProcess = slCoreProcess;
        var jsonRpc = this.slCoreProcess.AttachJsonRpc(rpcDebugger);
        var slCoreJsonRpc = slCoreJsonRpcFactory.CreateSLCoreJsonRpc(jsonRpc);

        rpcDebugger.SetUpDebugger(jsonRpc);

        slCoreServiceProvider.SetCurrentConnection(slCoreJsonRpc);
        listenerSetUp.Setup(slCoreJsonRpc);

        ShutdownTask = jsonRpc.Completion;
        ServiceProvider = slCoreServiceProvider;
    }

    public void Dispose()
    {
        slCoreProcess.Dispose();
    }
}
