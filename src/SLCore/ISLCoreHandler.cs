/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Notification;

namespace SonarLint.VisualStudio.SLCore;

public interface ISLCoreHandler : IDisposable
{
    void EnableSloop();

    void ForceRestartSloop();

    void ShowNotificationIfNeeded();
}

[Export(typeof(ISLCoreHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class SLCoreHandler : ISLCoreHandler
{
    private readonly object lockObject = new();
    private readonly ISloopRestartFailedNotificationService notificationService;
    private readonly ISLCoreInstanceHandler slCoreInstanceHandler;
    private readonly IThreadHandling threadHandling;
    private readonly int maxStartsBeforeUserNotification;
    private int initiatedStartAtCount = 0;
    private CancellationTokenSource? activeRunTokenSource;
    private Task? activeRun;
    private bool disposed;

    [ImportingConstructor]
    public SLCoreHandler(
        ISLCoreInstanceHandler slCoreInstanceHandler,
        ISloopRestartFailedNotificationService notificationService,
        IThreadHandling threadHandling)
        : this(slCoreInstanceHandler, notificationService, 2, threadHandling)
    {
    }

    internal SLCoreHandler(
        ISLCoreInstanceHandler slCoreInstanceHandler,
        ISloopRestartFailedNotificationService notificationService,
        int maxStartsBeforeUserNotification,
        IThreadHandling threadHandling)
    {
        if (maxStartsBeforeUserNotification <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxStartsBeforeUserNotification));
        }

        this.notificationService = notificationService;
        this.slCoreInstanceHandler = slCoreInstanceHandler;
        this.threadHandling = threadHandling;
        this.maxStartsBeforeUserNotification = maxStartsBeforeUserNotification;
    }

    public void EnableSloop()
    {
        var thisRunTokenSource = new CancellationTokenSource();
        var thisRunToken = thisRunTokenSource.Token;

        lock (lockObject)
        {
            ThrowIfDisposed();

            if (activeRun != null)
            {
                return; // already active
            }

            activeRunTokenSource = thisRunTokenSource;
            activeRun = threadHandling.RunOnBackgroundThread(() => LaunchSlCoreLoopOnRunOnBackgroundThreadAsync(thisRunToken));
        }
    }

    private async Task LaunchSlCoreLoopOnRunOnBackgroundThreadAsync(CancellationToken thisRunToken)
    {
        while (!disposed && !thisRunToken.IsCancellationRequested)
        {
            if (ReachedAutoRestartLimit())
            {
                notificationService.Show(ForceRestartSloop);
                return;
            }
            await slCoreInstanceHandler.StartInstanceAsync(thisRunToken);
        }
    }

    public void ForceRestartSloop()
    {
        Task? runToAwait;
        lock (lockObject)
        {
            ThrowIfDisposed();

            activeRunTokenSource?.Cancel();
            activeRunTokenSource?.Dispose();
            activeRunTokenSource = null;
            runToAwait = activeRun;
            activeRun = null;
            initiatedStartAtCount = slCoreInstanceHandler.CurrentStartNumber;
        }

        threadHandling.RunOnBackgroundThread(async () =>
        {
            if (runToAwait != null)
            {
                await threadHandling.RunAsync(() => runToAwait);
            }
            EnableSloop();
            return 0;
        }).Forget();
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(SLCoreHandler));
        }
    }

    public void ShowNotificationIfNeeded()
    {
        if (!disposed && ReachedAutoRestartLimit())
        {
            notificationService.Show(ForceRestartSloop);
        }
    }

    private bool ReachedAutoRestartLimit()
    {
        lock (lockObject)
        {
            return slCoreInstanceHandler.CurrentStartNumber - initiatedStartAtCount >= maxStartsBeforeUserNotification;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        slCoreInstanceHandler.Dispose();
    }
}
