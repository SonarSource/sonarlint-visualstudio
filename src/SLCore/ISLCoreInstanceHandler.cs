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

internal interface ISLCoreInstanceHandler : IDisposable
{
    int CurrentStartNumber { get; }
    Task StartInstanceAsync();
}

[Export(typeof(ISLCoreInstanceHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class SLCoreInstanceHandler : ISLCoreInstanceHandler
{
    private readonly IAliveConnectionTracker aliveConnectionTracker;
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly ISLCoreInstanceFactory slCoreInstanceFactory;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;
    internal /* for testing */ ISLCoreInstanceHandle currentInstanceHandle = null;
    private bool disposed;
    public int CurrentStartNumber { get; private set; }

    [ImportingConstructor]
    public SLCoreInstanceHandler(ISLCoreInstanceFactory slCoreInstanceFactory,
        IAliveConnectionTracker aliveConnectionTracker,
        IActiveConfigScopeTracker activeConfigScopeTracker,
        IThreadHandling threadHandling,
        ILogger logger)
    {
        this.aliveConnectionTracker = aliveConnectionTracker;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.slCoreInstanceFactory = slCoreInstanceFactory;
        this.threadHandling = threadHandling;
        this.logger = logger;
    }

    public async Task StartInstanceAsync()
    {
        threadHandling.ThrowIfOnUIThread();

        if (disposed)
        {
            throw new ObjectDisposedException(nameof(SLCoreInstanceHandler));
        }

        if (currentInstanceHandle is not null)
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
            currentInstanceHandle = slCoreInstanceFactory.CreateInstance();
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
            await currentInstanceHandle.InitializeAsync();
            await currentInstanceHandle.ShutdownTask;
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
        currentInstanceHandle?.Dispose();
        currentInstanceHandle = null;
        activeConfigScopeTracker?.Reset();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        currentInstanceHandle?.Dispose();
        aliveConnectionTracker?.Dispose();
        activeConfigScopeTracker?.Dispose();
    }
}


