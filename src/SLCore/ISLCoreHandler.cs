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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Notification;

namespace SonarLint.VisualStudio.SLCore;

public interface ISLCoreHandler : IDisposable
{
    void EnableSloop();
}

[Export(typeof(ISLCoreHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class SLCoreHandler : ISLCoreHandler
{
    private readonly ISloopRestartFailedNotificationService notificationService;
    private readonly ISLCoreInstanceHandler slCoreInstanceHandler;
    private readonly IThreadHandling threadHandling;
    private readonly int maxAutoStartsBeforeManual;
    private int initiatedStartAtCount = 0;
    private bool disposed;

    [ImportingConstructor]
    public SLCoreHandler(ISLCoreInstanceHandler slCoreInstanceHandler,
        ISloopRestartFailedNotificationService notificationService,
        IThreadHandling threadHandling)
        : this(slCoreInstanceHandler, notificationService, 3, threadHandling)
    {
    }

    internal /* for testing */ SLCoreHandler(ISLCoreInstanceHandler slCoreInstanceHandler,
        ISloopRestartFailedNotificationService notificationService,
        int maxAutoStartsBeforeManual,
        IThreadHandling threadHandling)
    {
        if (maxAutoStartsBeforeManual <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAutoStartsBeforeManual));
        }

        this.notificationService = notificationService;
        this.slCoreInstanceHandler = slCoreInstanceHandler;
        this.threadHandling = threadHandling;
        this.maxAutoStartsBeforeManual = maxAutoStartsBeforeManual;
    }

    public void EnableSloop()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(SLCoreHandler));
        }
        
        threadHandling.RunOnBackgroundThread(async () =>
        {
            while (!disposed && slCoreInstanceHandler.CurrentStartNumber - initiatedStartAtCount < maxAutoStartsBeforeManual)
            {
                await slCoreInstanceHandler.StartInstanceAsync();
            }

            if (!disposed)
            {
                notificationService.Show(InitiateRestart);
            }
            
            return 0;
        }).Forget();
    }

    private void InitiateRestart()
    {
        initiatedStartAtCount = slCoreInstanceHandler.CurrentStartNumber;
        EnableSloop();
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
