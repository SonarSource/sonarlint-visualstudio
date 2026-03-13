/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Monitoring;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore;

internal interface ISLCoreInstanceHandler : IDisposable
{
    int CurrentStartNumber { get; }
    Task StartInstanceAsync(CancellationToken token);
}

[Export(typeof(ISLCoreInstanceHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal sealed class SLCoreInstanceHandler(
    ISLCoreInstanceFactory slCoreInstanceFactory,
    IAliveConnectionTracker aliveConnectionTracker,
    IActiveConfigScopeTracker activeConfigScopeTracker,
    IMonitoringService monitoringService,
    IThreadHandling threadHandling,
    ILogger logger)
    : ISLCoreInstanceHandler
{
    internal /* for testing */ ISLCoreInstanceHandle? CurrentInstanceHandle;
    private bool disposed;
    public int CurrentStartNumber { get; private set; }

    public async Task StartInstanceAsync(CancellationToken token)
    {
        threadHandling.ThrowIfOnUIThread();

        if (disposed)
        {
            throw new ObjectDisposedException(nameof(SLCoreInstanceHandler));
        }

        if (CurrentInstanceHandle is not null)
        {
            throw new InvalidOperationException(SLCoreStrings.SLCoreHandler_InstanceAlreadyRunning);
        }

        if (token.IsCancellationRequested || !TryCreateInstance())
        {
            return;
        }

        await LaunchInstanceAsync(token);
    }

    private bool TryCreateInstance()
    {
        CurrentStartNumber++;
        try
        {
            logger.WriteLine(SLCoreStrings.SLCoreHandler_CreatingInstance);
            CurrentInstanceHandle = slCoreInstanceFactory.CreateInstance();
        }
        catch (Exception e)
        {
            logger.WriteLine(SLCoreStrings.SLCoreHandler_CreatingInstanceError);
            logger.LogVerbose(e.ToString());
            return false;
        }

        return true;
    }

    private async Task LaunchInstanceAsync(CancellationToken token)
    {
        try
        {
            logger.WriteLine(SLCoreStrings.SLCoreHandler_StartingInstance);
            var shutdownTask = await CurrentInstanceHandle!.InitializeAsync();
            InitializeMonitoring();
            await await Task.WhenAny(shutdownTask, Task.Delay(Timeout.Infinite, token));
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

    private void InitializeMonitoring()
    {
        try
        {
            monitoringService.Init();
        }
        catch (Exception)
        {
            //Swallow errors for not supported VS versions
        }
    }

    private void HandleInstanceDeath()
    {
        if (disposed)
        {
            return;
        }
        CurrentInstanceHandle?.Dispose();
        CurrentInstanceHandle = null;
        activeConfigScopeTracker?.Reset();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        CurrentInstanceHandle?.Dispose();
        aliveConnectionTracker?.Dispose();
        activeConfigScopeTracker?.Dispose();
    }
}


