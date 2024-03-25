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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore;

public interface ISLCoreHandler : IDisposable
{
    int CurrentStartNumber { get; }
    Task StartInstanceAsync();
}

[Export(typeof(ISLCoreHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class SLCoreHandler : ISLCoreHandler
{
    private IAliveConnectionTracker aliveConnectionTracker;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private ISLCoreHandleFactory slCoreHandleFactory;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;
    internal /* for testing */ ISLCoreHandle currentHandle = null;
    private bool disposed;
    public int CurrentStartNumber { get; private set; }

    [ImportingConstructor]
    public SLCoreHandler(ISLCoreHandleFactory slCoreHandleFactory,
        IAliveConnectionTracker aliveConnectionTracker,
        IActiveConfigScopeTracker activeConfigScopeTracker,
        IThreadHandling threadHandling,
        ILogger logger)
    {
        this.aliveConnectionTracker = aliveConnectionTracker;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.slCoreHandleFactory = slCoreHandleFactory;
        this.threadHandling = threadHandling;
        this.logger = logger;
    }

    public async Task StartInstanceAsync()
    {
        threadHandling.ThrowIfOnUIThread();

        if (disposed)
        {
            throw new ObjectDisposedException(nameof(SLCoreHandler));
        }

        if (currentHandle is not null)
        {
            throw new InvalidOperationException(SLCoreStrings.SLCoreHandler_InstanceAlreadyRunning);
        }
        
        if (!TryCreateInstance())
        {
            return;
        }

        await LaunchInstanceAsync();
    }

    private bool TryCreateInstance()
    {
        CurrentStartNumber++;
        try
        {
            logger.WriteLine(SLCoreStrings.SLCoreHandler_CreatingInstance);
            currentHandle = slCoreHandleFactory.CreateInstance();
        }
        catch (Exception e)
        {
            logger.WriteLine(SLCoreStrings.SLCoreHandler_CreatingInstanceError);
            logger.LogVerbose(e.ToString());
            return false;
        }

        return true;
    }

    private async Task LaunchInstanceAsync()
    {
        try
        {
            logger.WriteLine(SLCoreStrings.SLCoreHandler_StartingInstance);
            await currentHandle.InitializeAsync();
            await currentHandle.ShutdownTask;
        }
        catch (Exception e)
        {
            logger.WriteLine(SLCoreStrings.SLCoreHandler_StartingInstanceError);
            logger.LogVerbose(e.ToString());
        }
        finally
        {
            logger.WriteLine(SLCoreStrings.SLCoreHandler_InstanceDied);
            HandleInstanceDeath();
        }
    }

    private void HandleInstanceDeath()
    {
        if (disposed)
        {
            return;
        }
        currentHandle?.Dispose();
        currentHandle = null;
        activeConfigScopeTracker?.Reset();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        currentHandle?.Dispose();
        slCoreHandleFactory = null;
        aliveConnectionTracker?.Dispose();
        aliveConnectionTracker = null;
        activeConfigScopeTracker?.Dispose();
        activeConfigScopeTracker = null;
    }
}


